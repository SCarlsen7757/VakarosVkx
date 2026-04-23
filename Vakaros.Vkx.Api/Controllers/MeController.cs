using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Auth;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Api.Models.Entities;
using Vakaros.Vkx.Shared.Dtos.Me;
using Vakaros.Vkx.Shared.Dtos.Stats;
using Vakaros.Vkx.Shared.Dtos.Tokens;

namespace Vakaros.Vkx.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/me")]
public class MeController(
    AppDbContext db,
    UserManager<AppUser> userManager,
    ICurrentUser currentUser,
    AuthOptions authOptions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        if (authOptions.IsSingleUser)
        {
            return Ok(new UserProfileDto(
                AuthConstants.SystemUserId,
                AuthConstants.SystemUserEmail,
                "Local User",
                [AuthConstants.AdminRole, AuthConstants.UserRole],
                DateTimeOffset.UtcNow));
        }
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString());
        if (user is null) return NotFound();
        var roles = await userManager.GetRolesAsync(user);
        return Ok(new UserProfileDto(user.Id, user.Email!, user.DisplayName, [.. roles], user.CreatedAt));
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        if (authOptions.IsSingleUser) return BadRequest();
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString());
        if (user is null) return NotFound();
        user.DisplayName = req.DisplayName;
        await userManager.UpdateAsync(user);
        return Ok();
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        if (authOptions.IsSingleUser) return BadRequest();
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString());
        if (user is null) return NotFound();
        var result = await userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        return Ok();
    }

    [HttpGet("stats")]
    public async Task<ActionResult<GlobalStatsDto>> GetStats(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var totalBoats = await db.Boats.CountAsync(b => b.OwnerUserId == userId, ct);
        var sessions = await db.Sessions.Where(s => s.OwnerUserId == userId).ToListAsync(ct);
        var sessionIds = sessions.Select(s => s.Id).ToList();
        var races = await db.Races.Where(r => sessionIds.Contains(r.SessionId)).ToListAsync(ct);

        var dto = new GlobalStatsDto(
            totalBoats, sessions.Count, races.Count,
            sessions.Count > 0 ? sessions.Sum(s => (s.EndedAt - s.StartedAt).TotalSeconds) : 0.0,
            races.Count > 0 ? races.Where(r => r.EndedAt.HasValue).Sum(r => (r.EndedAt!.Value - r.StartedAt).TotalSeconds) : 0.0,
            races.Count > 0 ? races.Where(r => r.EndedAt.HasValue).Sum(r => r.SailedDistanceMeters) : 0.0,
            races.Count > 0 ? races.Max(r => r.MaxSpeedOverGround) : 0f,
            sessions.Count > 0 ? sessions.Min(s => s.StartedAt) : null,
            sessions.Count > 0 ? sessions.Max(s => s.StartedAt) : null);
        return Ok(dto);
    }

    // ── Personal Access Tokens ──────────────────────────────────────────
    [HttpGet("tokens")]
    public async Task<ActionResult<List<PersonalAccessTokenDto>>> ListTokens(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var tokens = await db.PersonalAccessTokens
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PersonalAccessTokenDto(p.Id, p.Name, p.TokenPrefix, p.CreatedAt, p.ExpiresAt, p.LastUsedAt, p.RevokedAt))
            .ToListAsync(ct);
        return Ok(tokens);
    }

    [HttpPost("tokens")]
    public async Task<ActionResult<CreatePatResponse>> CreateToken([FromBody] CreatePatRequest req, CancellationToken ct)
    {
        if (authOptions.IsSingleUser) return BadRequest(new { error = "pats_disabled_in_single_user" });
        var token = PatHasher.Generate();
        var hash = PatHasher.Hash(token);
        var pat = new PersonalAccessToken
        {
            UserId = currentUser.UserId,
            Name = req.Name,
            TokenHash = hash,
            TokenPrefix = token.Length > 12 ? token.Substring(0, 12) : token,
            ExpiresAt = req.ExpiresInDays.HasValue ? DateTimeOffset.UtcNow.AddDays(req.ExpiresInDays.Value) : null,
        };
        db.PersonalAccessTokens.Add(pat);
        await db.SaveChangesAsync(ct);
        return Ok(new CreatePatResponse(
            new PersonalAccessTokenDto(pat.Id, pat.Name, pat.TokenPrefix, pat.CreatedAt, pat.ExpiresAt, null, null),
            token));
    }

    [HttpDelete("tokens/{id:guid}")]
    public async Task<IActionResult> RevokeToken(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var pat = await db.PersonalAccessTokens.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct);
        if (pat is null) return NotFound();
        pat.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
