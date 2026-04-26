using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Auth;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Api.Services;
using Vakaros.Vkx.Shared.Dtos.Races;
using Vakaros.Vkx.Shared.Dtos.Sessions;

namespace Vakaros.Vkx.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/sessions")]
public class SessionsController(
    AppDbContext db,
    VkxIngestionService ingestionService,
    ICurrentUser currentUser,
    SessionAuthorizer sessionAuth) : ControllerBase
{
    [HttpPost]
    [Authorize]
    [RequestSizeLimit(200_000_000)]
    public async Task<ActionResult<SessionDetailDto>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var ownerId = currentUser.UserId;

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var fileBytes = ms.ToArray();
        var contentHash = VkxIngestionService.ComputeHash(fileBytes);

        if (await ingestionService.IsDuplicateAsync(ownerId, contentHash, ct))
            return Conflict(new { message = "A session with the same file content has already been uploaded." });

        var session = await ingestionService.IngestAsync(ownerId, fileBytes, file.FileName, contentHash, ct);

        var dto = new SessionDetailDto(
            session.Id, session.BoatId, null, session.CourseId, null,
            session.FileName, session.ContentHash, session.FormatVersion,
            session.TelemetryRateHz, session.IsFixedToBodyFrame, session.IsPublic,
            session.StartedAt, session.EndedAt, session.UploadedAt, session.Notes,
            IsOwned: true,
            [.. session.Races.OrderBy(r => r.RaceNumber).Select(r => new RaceDto(
                r.RaceNumber, r.CourseId, r.Course?.Name,
                r.CountdownStartedAt, r.CountdownDurationSeconds,
                r.StartedAt, r.EndedAt,
                r.EndedAt.HasValue ? (r.EndedAt.Value - r.StartedAt).TotalSeconds : null,
                r.SailedDistanceMeters, r.MaxSpeedOverGround, r.Notes))]
        );
        return CreatedAtAction(nameof(GetById), new { id = session.Id }, dto);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<SessionSummaryDto>>> GetAll(
        [FromQuery] Guid? boatId,
        [FromQuery] Guid? courseId,
        [FromQuery] int? year,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        var visibleIds = sessionAuth.ReadableSessionIds();
        var userId = currentUser.IsAuthenticated ? currentUser.UserId : (Guid?)null;

        var query = db.Sessions.Where(s => visibleIds.Contains(s.Id))
            .Include(s => s.Boat)
            .Include(s => s.Course)
            .Include(s => s.Races)
            .Include(s => s.Shares).ThenInclude(sh => sh.Team)
            .AsQueryable();

        if (boatId.HasValue) query = query.Where(s => s.BoatId == boatId.Value);
        if (courseId.HasValue) query = query.Where(s => s.CourseId == courseId.Value);
        if (year.HasValue) query = query.Where(s => s.StartedAt.Year == year.Value);
        if (from.HasValue) query = query.Where(s => s.StartedAt >= from.Value);
        if (to.HasValue) query = query.Where(s => s.StartedAt <= to.Value);

        var sessions = await query
            .OrderByDescending(s => s.UploadedAt)
            .ToListAsync(ct);

        // For team-shared sessions, determine which teams the current user is a member of
        var userTeamIds = userId.HasValue
            ? await db.TeamMembers.Where(m => m.UserId == userId.Value).Select(m => m.TeamId).ToListAsync(ct)
            : [];

        var result = sessions.Select(s => new SessionSummaryDto(
            s.Id, s.BoatId, s.Boat?.Name,
            s.CourseId, s.Course?.Name,
            s.FileName, s.FormatVersion, s.TelemetryRateHz, s.IsFixedToBodyFrame,
            s.StartedAt, s.EndedAt, s.UploadedAt, s.Notes, s.Races.Count,
            IsOwned: userId.HasValue && s.OwnerUserId == userId.Value,
            IsPublic: s.IsPublic,
            SharedViaTeams: s.Shares
                .Where(sh => userTeamIds.Contains(sh.TeamId))
                .Select(sh => sh.Team.Name)
                .ToList()
        )).ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<SessionDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        if (!await sessionAuth.CanReadAsync(id, ct)) return NotFound();

        var session = await db.Sessions
            .Include(s => s.Boat)
            .Include(s => s.Course)
            .Include(s => s.Races.OrderBy(r => r.RaceNumber))
                .ThenInclude(r => r.Course)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (session is null) return NotFound();

        var isOwned = currentUser.IsAuthenticated && session.OwnerUserId == currentUser.UserId;
        return Ok(BuildDetail(session, isOwned));
    }

    [HttpPatch("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<SessionDetailDto>> Patch(Guid id, PatchSessionRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var session = await db.Sessions
            .Include(s => s.Boat)
            .Include(s => s.Course)
            .Include(s => s.Races.OrderBy(r => r.RaceNumber))
                .ThenInclude(r => r.Course)
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerUserId == userId, ct);
        if (session is null) return NotFound();

        if (request.BoatId.HasValue) session.BoatId = request.BoatId.Value;
        if (request.CourseId.HasValue) session.CourseId = request.CourseId.Value;
        if (request.Notes is not null) session.Notes = request.Notes;
        if (request.IsPublic.HasValue) session.IsPublic = request.IsPublic.Value;
        await db.SaveChangesAsync(ct);
        await db.Entry(session).Reference(s => s.Boat).LoadAsync(ct);
        await db.Entry(session).Reference(s => s.Course).LoadAsync(ct);
        return Ok(BuildDetail(session, isOwned: true));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.Id == id && s.OwnerUserId == userId, ct);
        if (session is null) return NotFound();
        db.Sessions.Remove(session);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static SessionDetailDto BuildDetail(Models.Entities.Session session, bool isOwned) =>
        new(
            session.Id, session.BoatId, session.Boat?.Name,
            session.CourseId, session.Course?.Name,
            session.FileName, session.ContentHash, session.FormatVersion,
            session.TelemetryRateHz, session.IsFixedToBodyFrame, session.IsPublic,
            session.StartedAt, session.EndedAt, session.UploadedAt, session.Notes,
            isOwned,
            [.. session.Races.Select(r => new RaceDto(
                r.RaceNumber, r.CourseId, r.Course?.Name,
                r.CountdownStartedAt, r.CountdownDurationSeconds,
                r.StartedAt, r.EndedAt,
                r.EndedAt.HasValue ? (r.EndedAt.Value - r.StartedAt).TotalSeconds : null,
                r.SailedDistanceMeters, r.MaxSpeedOverGround, r.Notes))]);
}
