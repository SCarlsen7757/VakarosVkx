using System.Security.Cryptography;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vakaros.Vkx.Api.Audit;
using Vakaros.Vkx.Api.Auth;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Api.Models.Entities;
using Vakaros.Vkx.Shared.Dtos.Teams;

namespace Vakaros.Vkx.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/teams")]
public class TeamsController(
    AppDbContext db,
    UserManager<AppUser> userManager,
    ICurrentUser currentUser,
    IAuditService audit,
    IOptions<WebOptions> webOptions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TeamDto>>> GetMyTeams(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var teams = await db.TeamMembers
            .Where(tm => tm.UserId == userId)
            .Select(tm => new TeamDto(
                tm.Team.Id, tm.Team.Name, tm.Team.CreatedAt,
                tm.Team.Members.Count, tm.Role.ToString()))
            .ToListAsync(ct);
        return Ok(teams);
    }

    [HttpPost]
    public async Task<ActionResult<TeamDto>> Create([FromBody] CreateTeamRequest req, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var team = new Team { Name = req.Name, CreatedByUserId = userId };
        db.Teams.Add(team);
        db.TeamMembers.Add(new TeamMember { TeamId = team.Id, UserId = userId, Role = TeamRole.Owner });
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("team.create", "team", team.Id.ToString(), ct: ct);
        return CreatedAtAction(nameof(GetById), new { teamId = team.Id },
            new TeamDto(team.Id, team.Name, team.CreatedAt, 1, TeamRole.Owner.ToString()));
    }

    [HttpGet("{teamId:guid}")]
    public async Task<ActionResult<TeamDetailDto>> GetById(Guid teamId, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var membership = await db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId, ct);
        if (membership is null) return NotFound();

        var team = await db.Teams
            .Include(t => t.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == teamId, ct);
        if (team is null) return NotFound();

        return Ok(new TeamDetailDto(team.Id, team.Name, team.CreatedAt,
            [.. team.Members.Select(m => new TeamMemberDto(m.UserId, m.User.Email!, m.User.DisplayName, m.Role.ToString(), m.JoinedAt))]));
    }

    [HttpPatch("{teamId:guid}")]
    public async Task<IActionResult> Update(Guid teamId, [FromBody] UpdateTeamRequest req, CancellationToken ct)
    {
        if (!await IsAdminAsync(teamId, ct)) return Forbid();
        var team = await db.Teams.FindAsync([teamId], ct);
        if (team is null) return NotFound();
        team.Name = req.Name;
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpDelete("{teamId:guid}")]
    public async Task<IActionResult> Delete(Guid teamId, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var membership = await db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId, ct);
        if (membership is null || membership.Role != TeamRole.Owner) return Forbid();
        var team = await db.Teams.FindAsync([teamId], ct);
        if (team is null) return NotFound();
        db.Teams.Remove(team);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("team.delete", "team", teamId.ToString(), ct: ct);
        return NoContent();
    }

    [HttpGet("{teamId:guid}/members")]
    public async Task<ActionResult<List<TeamMemberDto>>> GetMembers(Guid teamId, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (!await db.TeamMembers.AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId, ct)) return NotFound();
        var members = await db.TeamMembers
            .Where(m => m.TeamId == teamId)
            .Select(m => new TeamMemberDto(m.UserId, m.User.Email!, m.User.DisplayName, m.Role.ToString(), m.JoinedAt))
            .ToListAsync(ct);
        return Ok(members);
    }

    [HttpPatch("{teamId:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> UpdateMemberRole(Guid teamId, Guid memberId, [FromBody] UpdateMemberRoleRequest req, CancellationToken ct)
    {
        if (!await IsAdminAsync(teamId, ct)) return Forbid();
        if (!Enum.TryParse<TeamRole>(req.Role, ignoreCase: true, out var role)) return BadRequest();
        var member = await db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == memberId, ct);
        if (member is null) return NotFound();
        member.Role = role;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("team.role_change", "team_member", $"{teamId}:{memberId}", details: role.ToString(), ct: ct);
        return Ok();
    }

    [HttpDelete("{teamId:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid teamId, Guid memberId, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        // Allow self-removal or admin removal.
        var requester = await db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId, ct);
        if (requester is null) return NotFound();
        if (memberId != userId && requester.Role < TeamRole.Admin) return Forbid();

        var target = await db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == memberId, ct);
        if (target is null) return NotFound();
        // Prevent removing the last owner.
        if (target.Role == TeamRole.Owner)
        {
            var ownerCount = await db.TeamMembers.CountAsync(m => m.TeamId == teamId && m.Role == TeamRole.Owner, ct);
            if (ownerCount <= 1) return BadRequest(new { error = "cannot_remove_last_owner" });
        }
        db.TeamMembers.Remove(target);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{teamId:guid}/invites")]
    public async Task<ActionResult<TeamInviteDto>> Invite(Guid teamId, [FromBody] InviteMemberRequest req, CancellationToken ct)
    {
        if (!await IsAdminAsync(teamId, ct)) return Forbid();
        if (!Enum.TryParse<TeamRole>(req.Role, ignoreCase: true, out var role)) return BadRequest();

        var token = GenerateInviteToken();
        var invite = new TeamInvite
        {
            TeamId = teamId,
            Email = req.Email,
            Token = token,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        };
        db.TeamInvites.Add(invite);
        await db.SaveChangesAsync(ct);

        var url = $"{webOptions.Value.PublicBaseUrl.TrimEnd('/')}/invites/accept?token={token}";
        await audit.LogAsync("team.invite", "team", teamId.ToString(), details: req.Email, ct: ct);
        // Email infrastructure was removed in the scale-back. The team admin shares this URL out-of-band.
        return Ok(new TeamInviteWithUrlDto(invite.Id, invite.Email, role.ToString(), invite.CreatedAt, invite.ExpiresAt, url));
    }

    private async Task<bool> IsAdminAsync(Guid teamId, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var m = await db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId, ct);
        return m is not null && m.Role >= TeamRole.Admin;
    }

    private static string GenerateInviteToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/invites")]
public class InvitesController(AppDbContext db, ICurrentUser currentUser, IAuditService audit) : ControllerBase
{
    [HttpPost("{token}/accept")]
    public async Task<IActionResult> Accept(string token, CancellationToken ct)
    {
        var invite = await db.TeamInvites.FirstOrDefaultAsync(i => i.Token == token, ct);
        if (invite is null) return NotFound();
        if (invite.AcceptedAt is not null) return BadRequest(new { error = "already_accepted" });
        if (invite.ExpiresAt < DateTimeOffset.UtcNow) return BadRequest(new { error = "expired" });

        var userId = currentUser.UserId;
        var existing = await db.TeamMembers.AnyAsync(m => m.TeamId == invite.TeamId && m.UserId == userId, ct);
        if (!existing)
        {
            db.TeamMembers.Add(new TeamMember { TeamId = invite.TeamId, UserId = userId, Role = TeamRole.Member });
        }
        invite.AcceptedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("team.invite_accepted", "team", invite.TeamId.ToString(), ct: ct);
        return Ok(new { teamId = invite.TeamId });
    }
}
