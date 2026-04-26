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
            .Select(sh => new SessionShareDto(sh.SessionId, sh.TeamId, sh.Team.Name, sh.CreatedAt))
            .ToListAsync(ct);
        return Ok(shares);
    }

    [HttpPut]
    public async Task<ActionResult<SessionShareDto>> CreateOrUpdate(Guid sessionId, [FromBody] CreateShareRequest req, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (!await db.Sessions.AnyAsync(s => s.Id == sessionId && s.OwnerUserId == userId, ct)) return NotFound();

        // Owner must be a member of the target team
        var isMember = await db.TeamMembers.AnyAsync(m => m.TeamId == req.TeamId && m.UserId == userId, ct);
        if (!isMember) return BadRequest(new { error = "not_team_member" });

        var existing = await db.SessionShares.FirstOrDefaultAsync(sh => sh.SessionId == sessionId && sh.TeamId == req.TeamId, ct);
        if (existing is null)
        {
            existing = new SessionShare { SessionId = sessionId, TeamId = req.TeamId };
            db.SessionShares.Add(existing);
        }
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("session.share", "session", sessionId.ToString(), details: req.TeamId.ToString(), ct: ct);

        var teamName = await db.Teams.Where(t => t.Id == req.TeamId).Select(t => t.Name).FirstAsync(ct);
        return Ok(new SessionShareDto(sessionId, req.TeamId, teamName, existing.CreatedAt));
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
