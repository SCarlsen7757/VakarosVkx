# Phase 1 status

✅ **COMPLETE (2026-04-23)** — REST /api/v1 cleanup + Guid (UUID v7) IDs.

Highlights:
- All entity/DTO IDs are now `Guid` with `Guid.CreateVersion7()` initializers; `RaceNumber` and `Year` stay `int`.
- `AppDbContext` adds `.ValueGeneratedNever()` on all 9 PKs.
- Controllers re-prefixed under `/api/v1`; route constraints `:int` → `:guid`.
- Telemetry channels grouped under `/races/{n}/telemetry/{name}`; start-line under `/races/{n}/analysis/start-line-length`.
- `POST /api/v1/sessions` (replaces `/api/Sessions/upload`).
- `StatsController` → `MeController` at `/api/v1/me/stats`.
- Migrations regenerated; web URLs bulk-updated; `api-types.ts` regenerated.
- Verified: `dotnet build` ✅ and `tsc --noEmit` ✅.

Phases 2–7 remain pending (Identity/auth, ownership, teams, BFF hardening, etc.).

---
# Multi-User, Teams & Security Upgrade — Vakaros.Vkx

## Problem statement

Today the app is a single-user, self-hosted tool: no authentication, no
ownership of data, all REST endpoints are open, IDs are enumerable `int`s,
and the API surface has some inconsistencies (`POST /api/sessions/upload`,
race resources hanging under sessions without versioning).

We want to evolve it to a **secure multi-user app** with **teams** and a
**clean, versioned REST API**, while keeping the **single-binary,
self-hostable docker-compose** experience intact.

## Confirmed design decisions

| Area | Decision |
| --- | --- |
| Auth | ASP.NET Core Identity + cookie auth + Next.js BFF (proxy) |
| Auth packaging | Pluggable. Modes: `single-user` (no login) / `multi-user` (Identity) — controlled by config |
| Login methods | **Local password** (email + password, requires SMTP for verification) AND **social login** via Google and GitHub (no SMTP needed). Each is independently toggleable in config. |
| Registration | Open registration. Local accounts require email verification before first login. Social-login accounts are auto-verified (we trust the provider's verified email). |
| MFA | Optional TOTP (authenticator app) per user — applies to local accounts; social-login users rely on the provider's MFA |
| Ownership | Per-user ownership of all domain entities (sessions, boats, courses, marks, boat classes) |
| Sharing | Owner can share a session with one or more teams. Team members get read-only by default; owner can grant edit. |
| Teams | Users belong to many teams; team has Owner / Admin / Member roles |
| Resource IDs | UUID v7 (sortable GUIDs) for all new public IDs |
| API versioning | New routes under `/api/v1/...`, restructure smelly routes |
| File storage | Keep current behavior (parse vkx, store derived data in DB; raw bytes not persisted long-term) |
| Existing data | Wipe & start fresh on upgrade — documented as a breaking change |

## Architecture overview

### Login options for self-hosters

A self-hoster picks one or both of these at deploy time. **No domain or SMTP
is required if only social login is enabled.**

| Option | Requires | Notes |
| --- | --- | --- |
| Google login | A free Google Cloud OAuth client ID | Easiest path for self-host. No SMTP. No domain. Just a callback URL. |
| GitHub login | A free GitHub OAuth App | Same as above. Great for dev/sailing tinkerers. |
| Generic OIDC | Any OIDC provider (Authentik, Keycloak, Auth0, Cognito) | Future-proof; lets the operator bring their own IdP. |
| Local password | SMTP relay (Gmail SMTP, SendGrid, Mailgun, SES, …) | Needed for email verification + password reset. Disabled by default if no SMTP is configured. |

If `Auth:Local:Enabled=true` but no SMTP is configured, the API refuses
to start with a clear error pointing the operator at the docs (or to use
social login instead).

When a user signs in with Google/GitHub for the first time:
1. We look up `AspNetUserLogins(Provider, ProviderKey)`.
2. If not found, we look up `AspNetUsers.Email`. If a local user with the
   same (verified) email exists, we **link** the external login to it
   (account linking, opt-in confirmation on the next login).
3. Otherwise we create a new Identity user with `EmailConfirmed=true`
   and link the external login.

### Auth / BFF flow

```
Browser ──HttpOnly cookie──► Next.js (BFF) ──forwards cookie──► .NET API
                                                              │
                                            ASP.NET Core Identity (cookies)
                                                              │
                                                          Postgres
```

- Browser only ever talks to Next.js (`/api/*` routes proxy to API).
- Cookie is `HttpOnly`, `Secure`, `SameSite=Lax`, scoped to the API host
  (when same-origin via the BFF proxy, it just works).
- Next.js server-side fetches attach the incoming cookie to the upstream
  request — no token ever reaches client JS.
- CSRF: double-submit cookie + `Origin`/`Referer` check on state-changing
  requests at the API.

### Authorization model

- Every owned entity gets `OwnerUserId` (FK → `AspNetUsers.Id`).
- Sharing is expressed in a join table:
  - `SessionShares(SessionId, TeamId, Permission)` where `Permission ∈ {Read, Write}`.
- A request can access a session iff:
  1. The user is the owner, OR
  2. The user is a member of a team that has a share row for that session.
- Cascading rule for derived data (races, telemetry hypertables): inherit
  the parent session's authz — no separate row-level checks needed because
  they're always queried via the session scope.
- Cross-cutting: a single `IAuthorizationService`-backed
  `SessionAccessRequirement` keeps the policy in one place.

### Single-user mode

- `Auth:Mode=SingleUser` → `Program.cs` skips Identity, registers a
  middleware that injects a synthetic `system` user into `HttpContext.User`,
  and all queries still filter by `OwnerUserId == systemUserId`.
- This keeps the data model identical and removes branching in controllers.

### Teams

- `Teams(Id, Name, CreatedAt, CreatedByUserId)`
- `TeamMembers(TeamId, UserId, Role, JoinedAt)` — `Role ∈ {Owner, Admin, Member}`
- Invites: `TeamInvites(Id, TeamId, Email, Token, ExpiresAt, AcceptedAt)`.
  Email-based invite link, token is a one-time secret, expires in 7 days.

### Security hardening (Web↔API)

- HTTPS-only (HSTS, secure cookies).
- BFF proxy: API only accepts requests from the Next.js origin (CORS off
  for direct browser access; `AllowedOrigins` config for the BFF).
- CSRF tokens on state-changing routes.
- Rate limiting: per-IP and per-user buckets via
  `Microsoft.AspNetCore.RateLimiting` (login: strict; uploads: medium;
  reads: lenient).
- Account lockout (Identity defaults: 5 attempts / 15 min).
- Password policy: 12+ chars, complexity off but length-enforced + HIBP
  pwned-password check (optional pluggable).
- Audit log table: `AuditEvents(Id, UserId, Action, EntityType, EntityId, At, IpAddress)`
  for login, share, delete, role change.
- Security headers middleware: `X-Content-Type-Options`,
  `Referrer-Policy`, `Content-Security-Policy`, `X-Frame-Options=DENY`.
- Email-confirmed-required policy globally; unconfirmed users can only
  hit the resend-verification endpoint.
- Personal Access Tokens (PAT) for the Console / CI uploads — opaque
  tokens hashed at rest, revocable, scoped to a user.

## REST API redesign (v1)

Old → new mapping (illustrative; not exhaustive):

| Old | New |
| --- | --- |
| `POST /api/sessions/upload` | `POST /api/v1/sessions` (multipart) |
| `GET  /api/sessions` | `GET /api/v1/sessions` |
| `PATCH /api/sessions/{id}` | `PATCH /api/v1/sessions/{id}` |
| `GET  /api/sessions/{id}/races/{n}/positions` | `GET /api/v1/sessions/{sessionId}/races/{raceNumber}/telemetry/positions` |
| `GET .../speed-through-water` | `GET .../telemetry/speed-through-water` (consistent grouping) |
| `GET .../start-line-length` | `GET .../analysis/start-line-length` |
| `POST/GET/DELETE /api/racesummary` | `POST/GET/DELETE /api/v1/sessions/{sessionId}/races/{raceNumber}/summary` |
| `GET /api/stats/summary` | `GET /api/v1/me/stats` (per-user) |

New auth routes:

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/v1/auth/register` | Local register (sends verification email) |
| `POST` | `/api/v1/auth/login` | Local login (sets cookie) |
| `POST` | `/api/v1/auth/logout` | Logout |
| `POST` | `/api/v1/auth/verify-email` | Confirm email via token |
| `POST` | `/api/v1/auth/resend-verification` | |
| `POST` | `/api/v1/auth/forgot-password` / `reset-password` | |
| `GET`  | `/api/v1/auth/providers` | Lists enabled login methods (so the UI can show only the configured buttons) |
| `GET`  | `/api/v1/auth/external/{provider}/start` | Begins external login (Google / GitHub) |
| `GET`  | `/api/v1/auth/external/{provider}/callback` | OAuth callback; creates or links user, sets cookie |
| `POST` | `/api/v1/me/external-logins` / `DELETE /…/{provider}` | Link or unlink a social provider on the current account |
| `GET`  | `/api/v1/me` | Current user profile |
| `POST` | `/api/v1/me/mfa/enable` / `disable` / `verify` | TOTP |
| `GET/POST/DELETE` | `/api/v1/me/tokens` | PATs |

New teams + sharing routes:

| Method | Route |
| --- | --- |
| `GET/POST` | `/api/v1/teams` |
| `GET/PATCH/DELETE` | `/api/v1/teams/{teamId}` |
| `GET` | `/api/v1/teams/{teamId}/members` |
| `POST` | `/api/v1/teams/{teamId}/invites` |
| `POST` | `/api/v1/invites/{token}/accept` |
| `DELETE` | `/api/v1/teams/{teamId}/members/{userId}` |
| `GET/PUT/DELETE` | `/api/v1/sessions/{sessionId}/shares` |

Conventions:
- All IDs `Guid` in URLs (UUID v7 generated server-side).
- Plural nouns, kebab-case for multi-word segments.
- Pagination: `?page=&pageSize=` with `X-Total-Count`.
- Errors: RFC 7807 `application/problem+json`.
- All write endpoints require auth + share/owner check; reads filtered
  by visibility.

## Data model changes

New tables (ASP.NET Identity adds the standard `aspnet_*` set):

- `teams`
- `team_members`
- `team_invites`
- `session_shares`
- `personal_access_tokens`
- `audit_events`

Schema changes to existing tables:

- Add `owner_user_id uuid NOT NULL` to: `boats`, `boat_classes`, `sails`,
  `marks`, `courses`, `course_legs` (inherited via course), `sessions`,
  `race_summary_reports`.
- Composite indexes `(owner_user_id, ...)` for hot list queries.
- Switch primary keys from `int` to `uuid` (UUID v7). Hypertable composite
  PKs (`time, session_id`) become `(time, session_id::uuid)`.
- `Sessions.ContentHash` uniqueness becomes scoped to owner:
  `UNIQUE (owner_user_id, content_hash)` (two users can upload the same
  file independently).

Migration strategy:
- New EF Core migration set; document the upgrade as **breaking** in
  README — operators drop the volume and re-upload.

## Frontend (Next.js) changes

- Add an auth context: `/login`, `/register`, `/verify-email`,
  `/forgot-password`, `/reset-password`, `/account` (profile, MFA, PATs,
  linked social accounts).
- `/login` queries `/api/v1/auth/providers` and only renders buttons for
  the providers the operator enabled (e.g. just "Sign in with Google" if
  local auth is off).
- Convert the existing API proxy in `src/app/api/[...path]/route.ts`
  into a real BFF: forward cookies, strip hop-by-hop headers, attach
  CSRF token, never expose API base URL to the browser.
- Add a session-aware layout: redirect unauthenticated users to `/login`
  except for public pages.
- Teams UI: `/teams`, `/teams/[id]` (members, invites).
- Share dialog on session detail page: pick teams + permission.
- "Mode banner" when running in single-user mode (so self-hosters know).

## Configuration (appsettings)

```json
{
  "Auth": {
    "Mode": "MultiUser",          // or "SingleUser"
    "Cookie": { "Name": "vkx.auth", "SlidingExpirationDays": 14 },
    "Local": {
      "Enabled": true,             // set false if you have no SMTP
      "RequireEmailConfirmed": true
    },
    "External": {
      "Google": { "Enabled": false, "ClientId": "...", "ClientSecret": "..." },
      "GitHub": { "Enabled": false, "ClientId": "...", "ClientSecret": "..." }
      // future: generic OIDC block here
    }
  },
  "Email": {
    "Provider": "Smtp",            // Smtp | Console (dev) | None
    "Smtp": { "Host": "...", "Port": 587, "User": "...", "Pass": "...", "From": "no-reply@example.com" }
  },
  "Cors": { "AllowedOrigins": ["https://web.example.com"] },
  "RateLimit": { "Login": "5/min", "Upload": "20/hour", "Read": "120/min" }
}
```

`docker-compose.yml` additions:
- `MAILHOG`/`mailpit` service for local email viewing.
- Env vars wired for `Auth__Mode`, SMTP, and a generated data-protection
  key directory mounted as a volume (so cookies survive restarts).
- `DataProtection` keys persisted to a named volume.

## Testing

- Unit tests for the new authorization service (owner / team / public
  matrix).
- Integration tests with `WebApplicationFactory`: register → verify →
  login → upload → share → second user reads.
- E2E (existing tooling, if any) for the login + session-upload flow.
- Security tests: CSRF reject without token, rate-limit triggers, IDOR
  attempts return 404 (not 403, to avoid existence leak).

## Roll-out / phasing

Implementation will proceed in phases (each independently mergeable):

1. **Foundations** — versioned routes, GUID IDs, REST cleanup (no auth yet).
2. **Identity & cookie auth** — register/login/email/MFA, single-user mode flag.
2b. **Social login (Google + GitHub)** — external login providers, account linking, no-SMTP self-host path.
3. **Ownership** — `OwnerUserId` on all entities, query filters, authz policy.
4. **Teams & sharing** — teams, members, invites, session shares.
5. **BFF & web auth UI** — Next.js proxy hardening, login/register pages,
   share dialog, teams UI.
6. **Hardening** — rate limits, CSP, audit log, PATs, security tests.
7. **Docs** — updated README, breaking-change notice, self-host quickstart.

## Out of scope (for this plan)

- AWS-specific deployment (ECS/RDS/Cognito) — deferred to a follow-up plan.
- File storage abstraction / S3.
- Mobile clients, third-party OIDC clients (OpenIddict server mode).
- Migrating existing single-user data — explicit "wipe & re-upload".
