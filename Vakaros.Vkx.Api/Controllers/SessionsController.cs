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
[Authorize]
[Route("api/v{version:apiVersion}/sessions")]
public class SessionsController(
    AppDbContext db,
    VkxIngestionService ingestionService,
    ICurrentUser currentUser,
    SessionAuthorizer sessionAuth) : ControllerBase
{
    [HttpPost]
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
            session.TelemetryRateHz, session.IsFixedToBodyFrame,
            session.StartedAt, session.EndedAt, session.UploadedAt, session.Notes,
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
    public async Task<ActionResult<List<SessionSummaryDto>>> GetAll(
        [FromQuery] Guid? boatId,
        [FromQuery] Guid? courseId,
        [FromQuery] int? year,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        var visibleIds = sessionAuth.ReadableSessionIds();
        var query = db.Sessions.Where(s => visibleIds.Contains(s.Id))
            .Include(s => s.Boat)
            .Include(s => s.Course)
            .Include(s => s.Races)
            .AsQueryable();

        if (boatId.HasValue) query = query.Where(s => s.BoatId == boatId.Value);
        if (courseId.HasValue) query = query.Where(s => s.CourseId == courseId.Value);
        if (year.HasValue) query = query.Where(s => s.StartedAt.Year == year.Value);
        if (from.HasValue) query = query.Where(s => s.StartedAt >= from.Value);
        if (to.HasValue) query = query.Where(s => s.StartedAt <= to.Value);

        var sessions = await query
            .OrderByDescending(s => s.UploadedAt)
            .Select(s => new SessionSummaryDto(
                s.Id, s.BoatId, s.Boat != null ? s.Boat.Name : null,
                s.CourseId, s.Course != null ? s.Course.Name : null,
                s.FileName, s.FormatVersion, s.TelemetryRateHz, s.IsFixedToBodyFrame,
                s.StartedAt, s.EndedAt, s.UploadedAt, s.Notes, s.Races.Count))
            .ToListAsync(ct);
        return Ok(sessions);
    }

    [HttpGet("{id:guid}")]
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

        return Ok(BuildDetail(session));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<SessionDetailDto>> Patch(Guid id, PatchSessionRequest request, CancellationToken ct)
    {
        if (!await sessionAuth.CanWriteAsync(id, ct)) return NotFound();

        var session = await db.Sessions
            .Include(s => s.Boat)
            .Include(s => s.Course)
            .Include(s => s.Races.OrderBy(r => r.RaceNumber))
                .ThenInclude(r => r.Course)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (session is null) return NotFound();

        if (request.BoatId.HasValue) session.BoatId = request.BoatId.Value;
        if (request.CourseId.HasValue) session.CourseId = request.CourseId.Value;
        if (request.Notes is not null) session.Notes = request.Notes;
        await db.SaveChangesAsync(ct);
        await db.Entry(session).Reference(s => s.Boat).LoadAsync(ct);
        await db.Entry(session).Reference(s => s.Course).LoadAsync(ct);
        return Ok(BuildDetail(session));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.Id == id && s.OwnerUserId == userId, ct);
        if (session is null) return NotFound();
        db.Sessions.Remove(session);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static SessionDetailDto BuildDetail(Models.Entities.Session session) =>
        new(
            session.Id, session.BoatId, session.Boat?.Name,
            session.CourseId, session.Course?.Name,
            session.FileName, session.ContentHash, session.FormatVersion,
            session.TelemetryRateHz, session.IsFixedToBodyFrame,
            session.StartedAt, session.EndedAt, session.UploadedAt, session.Notes,
            [.. session.Races.Select(r => new RaceDto(
                r.RaceNumber, r.CourseId, r.Course?.Name,
                r.CountdownStartedAt, r.CountdownDurationSeconds,
                r.StartedAt, r.EndedAt,
                r.EndedAt.HasValue ? (r.EndedAt.Value - r.StartedAt).TotalSeconds : null,
                r.SailedDistanceMeters, r.MaxSpeedOverGround, r.Notes))]);
}
