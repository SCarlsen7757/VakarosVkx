using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Audit;
using Vakaros.Vkx.Api.Auth;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Api.Models.Entities;
using Vakaros.Vkx.Shared.Dtos.Shares;

namespace Vakaros.Vkx.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/sessions/{sessionId:guid}/shares")]
public class SharesController(AppDbContext db, ICurrentUser currentUser, IAuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SessionShareDto>>> GetShares(Guid sessionId, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (!await db.Sessions.AnyAsync(s => s.Id == sessionId && s.OwnerUserId == userId, ct)) return NotFound();
        var shares = await db.SessionShares
            .Where(sh => sh.SessionId == sessionId)
            .Select(sh => new SessionShareDto(sh.SessionId, sh.TeamId, sh.Team.Name, sh.Permission.ToString(), sh.CreatedAt))
            .ToListAsync(ct);
        return Ok(shares);
    }

    [HttpPut]
    public async Task<ActionResult<SessionShareDto>> CreateOrUpdate(Guid sessionId, [FromBody] CreateOrUpdateShareRequest req, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (!await db.Sessions.AnyAsync(s => s.Id == sessionId && s.OwnerUserId == userId, ct)) return NotFound();
        if (!Enum.TryParse<SharePermission>(req.Permission, ignoreCase: true, out var perm)) return BadRequest();
        if (!await db.Teams.AnyAsync(t => t.Id == req.TeamId, ct)) return BadRequest(new { error = "unknown_team" });

        var existing = await db.SessionShares.FirstOrDefaultAsync(sh => sh.SessionId == sessionId && sh.TeamId == req.TeamId, ct);
        if (existing is null)
        {
            existing = new SessionShare { SessionId = sessionId, TeamId = req.TeamId, Permission = perm };
            db.SessionShares.Add(existing);
        }
        else
        {
            existing.Permission = perm;
        }
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("session.share", "session", sessionId.ToString(), details: $"{req.TeamId}:{perm}", ct: ct);

        var teamName = await db.Teams.Where(t => t.Id == req.TeamId).Select(t => t.Name).FirstAsync(ct);
        return Ok(new SessionShareDto(sessionId, req.TeamId, teamName, perm.ToString(), existing.CreatedAt));
    }

    [HttpDelete("{teamId:guid}")]
    public async Task<IActionResult> Delete(Guid sessionId, Guid teamId, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (!await db.Sessions.AnyAsync(s => s.Id == sessionId && s.OwnerUserId == userId, ct)) return NotFound();
        var share = await db.SessionShares.FirstOrDefaultAsync(sh => sh.SessionId == sessionId && sh.TeamId == teamId, ct);
        if (share is null) return NotFound();
        db.SessionShares.Remove(share);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("session.unshare", "session", sessionId.ToString(), details: teamId.ToString(), ct: ct);
        return NoContent();
    }
}
