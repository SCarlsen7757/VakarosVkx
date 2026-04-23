using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Data;

namespace Vakaros.Vkx.Api.Auth;

/// <summary>
/// Helper for checking that a session is accessible to the current user.
/// Returns true when the user is owner OR the session is shared via a team membership.
/// </summary>
public sealed class SessionAuthorizer(AppDbContext db, ICurrentUser currentUser, AuthOptions auth)
{
    public Task<bool> CanReadAsync(Guid sessionId, CancellationToken ct = default) => CanAsync(sessionId, write: false, ct);
    public Task<bool> CanWriteAsync(Guid sessionId, CancellationToken ct = default) => CanAsync(sessionId, write: true, ct);

    private async Task<bool> CanAsync(Guid sessionId, bool write, CancellationToken ct)
    {
        if (auth.IsSingleUser) return await db.Sessions.AnyAsync(s => s.Id == sessionId, ct);
        if (!currentUser.IsAuthenticated) return false;

        var userId = currentUser.UserId;
        var isOwner = await db.Sessions.AnyAsync(s => s.Id == sessionId && s.OwnerUserId == userId, ct);
        if (isOwner) return true;

        var min = write ? Models.Entities.SharePermission.Write : Models.Entities.SharePermission.Read;
        return await (
            from share in db.SessionShares
            where share.SessionId == sessionId && share.Permission >= min
            join member in db.TeamMembers on share.TeamId equals member.TeamId
            where member.UserId == userId
            select share.SessionId).AnyAsync(ct);
    }

    /// <summary>
    /// Returns the queryable of session IDs the current user can read (owned + team-shared).
    /// </summary>
    public IQueryable<Guid> ReadableSessionIds()
    {
        if (auth.IsSingleUser) return db.Sessions.Select(s => s.Id);
        var userId = currentUser.UserId;
        var owned = db.Sessions.Where(s => s.OwnerUserId == userId).Select(s => s.Id);
        var shared = db.SessionShares
            .Where(sh => db.TeamMembers.Any(m => m.TeamId == sh.TeamId && m.UserId == userId))
            .Select(sh => sh.SessionId);
        return owned.Union(shared);
    }
}
