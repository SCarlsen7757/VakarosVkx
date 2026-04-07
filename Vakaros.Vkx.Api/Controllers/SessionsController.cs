using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Api.Models.Dtos;
using Vakaros.Vkx.Api.Services;

namespace Vakaros.Vkx.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController(AppDbContext db, VkxIngestionService ingestionService) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(200_000_000)] // 200 MB
    public async Task<ActionResult<SessionDetailDto>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var fileBytes = ms.ToArray();

        var contentHash = VkxIngestionService.ComputeHash(fileBytes);

        if (await ingestionService.IsDuplicateAsync(contentHash, ct))
            return Conflict(new { message = "A session with the same file content has already been uploaded." });

        var session = await ingestionService.IngestAsync(fileBytes, file.FileName, contentHash, ct);

        var dto = new SessionDetailDto(
            session.Id,
            session.BoatId,
            null,
            session.CourseId,
            null,
            session.FileName,
            session.ContentHash,
            session.FormatVersion,
            session.TelemetryRateHz,
            session.IsFixedToBodyFrame,
            session.StartedAt,
            session.EndedAt,
            session.UploadedAt,
            session.Notes,
            [.. session.Races.OrderBy(r => r.RaceNumber).Select(r => new RaceDto(
                r.RaceNumber,
                r.StartedAt,
                r.EndedAt,
                (r.EndedAt - r.StartedAt).TotalSeconds))]
        );

        return CreatedAtAction(nameof(GetById), new { id = session.Id }, dto);
    }

    [HttpGet]
    public async Task<ActionResult<List<SessionSummaryDto>>> GetAll(
        [FromQuery] int? boatId,
        [FromQuery] int? courseId,
        [FromQuery] int? year,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        var query = db.Sessions
            .Include(s => s.Boat)
            .Include(s => s.Course)
            .Include(s => s.Races)
            .AsQueryable();

        if (boatId.HasValue)
            query = query.Where(s => s.BoatId == boatId.Value);
        if (courseId.HasValue)
            query = query.Where(s => s.CourseId == courseId.Value);
        if (year.HasValue)
            query = query.Where(s => s.StartedAt.Year == year.Value);
        if (from.HasValue)
            query = query.Where(s => s.StartedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(s => s.StartedAt <= to.Value);

        var sessions = await query
            .OrderByDescending(s => s.UploadedAt)
            .Select(s => new SessionSummaryDto(
                s.Id,
                s.BoatId,
                s.Boat != null ? s.Boat.Name : null,
                s.CourseId,
                s.Course != null ? s.Course.Name : null,
                s.FileName,
                s.FormatVersion,
                s.TelemetryRateHz,
                s.IsFixedToBodyFrame,
                s.StartedAt,
                s.EndedAt,
                s.UploadedAt,
                s.Notes,
                s.Races.Count))
            .ToListAsync(ct);

        return Ok(sessions);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SessionDetailDto>> GetById(int id, CancellationToken ct)
    {
        var session = await db.Sessions
            .Include(s => s.Boat)
            .Include(s => s.Course)
            .Include(s => s.Races.OrderBy(r => r.RaceNumber))
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (session is null) return NotFound();

        var dto = new SessionDetailDto(
            session.Id,
            session.BoatId,
            session.Boat?.Name,
            session.CourseId,
            session.Course?.Name,
            session.FileName,
            session.ContentHash,
            session.FormatVersion,
            session.TelemetryRateHz,
            session.IsFixedToBodyFrame,
            session.StartedAt,
            session.EndedAt,
            session.UploadedAt,
            session.Notes,
            [.. session.Races.Select(r => new RaceDto(
                r.RaceNumber,
                r.StartedAt,
                r.EndedAt,
                (r.EndedAt - r.StartedAt).TotalSeconds))]
        );

        return Ok(dto);
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<SessionDetailDto>> Patch(int id, PatchSessionRequest request, CancellationToken ct)
    {
        var session = await db.Sessions
            .Include(s => s.Boat)
            .Include(s => s.Course)
            .Include(s => s.Races.OrderBy(r => r.RaceNumber))
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (session is null) return NotFound();

        if (request.BoatId.HasValue)
            session.BoatId = request.BoatId.Value;
        if (request.CourseId.HasValue)
            session.CourseId = request.CourseId.Value;
        if (request.Notes is not null)
            session.Notes = request.Notes;

        await db.SaveChangesAsync(ct);

        // Reload navigations after FK change.
        await db.Entry(session).Reference(s => s.Boat).LoadAsync(ct);
        await db.Entry(session).Reference(s => s.Course).LoadAsync(ct);

        var dto = new SessionDetailDto(
            session.Id,
            session.BoatId,
            session.Boat?.Name,
            session.CourseId,
            session.Course?.Name,
            session.FileName,
            session.ContentHash,
            session.FormatVersion,
            session.TelemetryRateHz,
            session.IsFixedToBodyFrame,
            session.StartedAt,
            session.EndedAt,
            session.UploadedAt,
            session.Notes,
            [.. session.Races.Select(r => new RaceDto(
                r.RaceNumber,
                r.StartedAt,
                r.EndedAt,
                (r.EndedAt - r.StartedAt).TotalSeconds))]
        );

        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var session = await db.Sessions.FindAsync([id], ct);
        if (session is null) return NotFound();

        db.Sessions.Remove(session);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}
