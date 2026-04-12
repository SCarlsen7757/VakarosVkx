using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Api.Models.Entities;
using Vakaros.Vkx.Shared.Dtos.Races;
using Vakaros.Vkx.Shared.Dtos.Telemetry;

namespace Vakaros.Vkx.Api.Controllers;

[ApiController]
[Route("api/sessions/{sessionId:int}/races")]
public class RacesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RaceDto>>> GetAll(int sessionId, CancellationToken ct)
    {
        var sessionExists = await db.Sessions.AnyAsync(s => s.Id == sessionId, ct);
        if (!sessionExists) return NotFound();

        var races = await db.Races
            .Where(r => r.SessionId == sessionId)
            .OrderBy(r => r.RaceNumber)
            .Select(r => new RaceDto(
                r.RaceNumber,
                r.CourseId,
                r.Course != null ? r.Course.Name : null,
                r.StartedAt,
                r.EndedAt,
                (r.EndedAt - r.StartedAt).TotalSeconds,
                r.SailedDistanceMeters,
                r.MaxSpeedOverGround))
            .ToListAsync(ct);

        return Ok(races);
    }

    [HttpGet("{raceNumber:int}")]
    public async Task<ActionResult<RaceDetailDto>> GetByNumber(int sessionId, int raceNumber, CancellationToken ct)
    {
        var race = await FindRaceAsync(sessionId, raceNumber, ct);
        if (race is null) return NotFound();

        // Get most recent start line positions at or before race start.
        var pinEnd = await db.LinePositions
            .Where(l => l.SessionId == sessionId && l.LineEnd == 0 && l.Time <= race.StartedAt)
            .OrderByDescending(l => l.Time)
            .Select(l => new LinePositionDto(l.Time, l.Latitude, l.Longitude))
            .FirstOrDefaultAsync(ct);

        var boatEnd = await db.LinePositions
            .Where(l => l.SessionId == sessionId && l.LineEnd == 1 && l.Time <= race.StartedAt)
            .OrderByDescending(l => l.Time)
            .Select(l => new LinePositionDto(l.Time, l.Latitude, l.Longitude))
            .FirstOrDefaultAsync(ct);

        var duration = (race.EndedAt - race.StartedAt).TotalSeconds;

        return Ok(new RaceDetailDto(race.RaceNumber, race.CourseId, race.StartedAt, race.EndedAt, duration, race.SailedDistanceMeters, race.MaxSpeedOverGround, pinEnd, boatEnd));
    }

    [HttpGet("{raceNumber:int}/positions")]
    public async Task<ActionResult<List<PositionDto>>> GetPositions(
        int sessionId, int raceNumber, [FromQuery] double? from, [FromQuery] double? to, CancellationToken ct)
    {
        var race = await FindRaceAsync(sessionId, raceNumber, ct);
        if (race is null) return NotFound();

        var (start, end) = ComputeTimeWindow(race, from, to);

        var positions = await db.Positions
            .Where(p => p.SessionId == sessionId && p.Time >= start && p.Time <= end)
            .OrderBy(p => p.Time)
            .Select(p => new PositionDto(p.Time, p.Latitude, p.Longitude, p.SpeedOverGround, p.CourseOverGround, p.Altitude, p.QuaternionW, p.QuaternionX, p.QuaternionY, p.QuaternionZ))
            .ToListAsync(ct);

        return Ok(positions);
    }

    [HttpGet("{raceNumber:int}/wind")]
    public async Task<ActionResult<List<WindDto>>> GetWind(
        int sessionId, int raceNumber, [FromQuery] double? from, [FromQuery] double? to, CancellationToken ct)
    {
        var race = await FindRaceAsync(sessionId, raceNumber, ct);
        if (race is null) return NotFound();

        var (start, end) = ComputeTimeWindow(race, from, to);

        var readings = await db.WindReadings
            .Where(w => w.SessionId == sessionId && w.Time >= start && w.Time <= end)
            .OrderBy(w => w.Time)
            .Select(w => new WindDto(w.Time, w.WindDirection, w.WindSpeed))
            .ToListAsync(ct);

        return Ok(readings);
    }

    [HttpGet("{raceNumber:int}/speed-through-water")]
    public async Task<ActionResult<List<SpeedThroughWaterDto>>> GetSpeedThroughWater(
        int sessionId, int raceNumber, [FromQuery] double? from, [FromQuery] double? to, CancellationToken ct)
    {
        var race = await FindRaceAsync(sessionId, raceNumber, ct);
        if (race is null) return NotFound();

        var (start, end) = ComputeTimeWindow(race, from, to);

        var readings = await db.SpeedThroughWater
            .Where(s => s.SessionId == sessionId && s.Time >= start && s.Time <= end)
            .OrderBy(s => s.Time)
            .Select(s => new SpeedThroughWaterDto(s.Time, s.ForwardSpeed, s.HorizontalSpeed))
            .ToListAsync(ct);

        return Ok(readings);
    }

    [HttpGet("{raceNumber:int}/depth")]
    public async Task<ActionResult<List<DepthDto>>> GetDepth(
        int sessionId, int raceNumber, [FromQuery] double? from, [FromQuery] double? to, CancellationToken ct)
    {
        var race = await FindRaceAsync(sessionId, raceNumber, ct);
        if (race is null) return NotFound();

        var (start, end) = ComputeTimeWindow(race, from, to);

        var readings = await db.DepthReadings
            .Where(d => d.SessionId == sessionId && d.Time >= start && d.Time <= end)
            .OrderBy(d => d.Time)
            .Select(d => new DepthDto(d.Time, d.Depth))
            .ToListAsync(ct);

        return Ok(readings);
    }

    [HttpGet("{raceNumber:int}/temperature")]
    public async Task<ActionResult<List<TemperatureDto>>> GetTemperature(
        int sessionId, int raceNumber, [FromQuery] double? from, [FromQuery] double? to, CancellationToken ct)
    {
        var race = await FindRaceAsync(sessionId, raceNumber, ct);
        if (race is null) return NotFound();

        var (start, end) = ComputeTimeWindow(race, from, to);

        var readings = await db.TemperatureReadings
            .Where(t => t.SessionId == sessionId && t.Time >= start && t.Time <= end)
            .OrderBy(t => t.Time)
            .Select(t => new TemperatureDto(t.Time, t.Temperature))
            .ToListAsync(ct);

        return Ok(readings);
    }

    [HttpGet("{raceNumber:int}/load")]
    public async Task<ActionResult<List<LoadDto>>> GetLoad(
        int sessionId, int raceNumber, [FromQuery] double? from, [FromQuery] double? to, CancellationToken ct)
    {
        var race = await FindRaceAsync(sessionId, raceNumber, ct);
        if (race is null) return NotFound();

        var (start, end) = ComputeTimeWindow(race, from, to);

        var readings = await db.LoadReadings
            .Where(l => l.SessionId == sessionId && l.Time >= start && l.Time <= end)
            .OrderBy(l => l.Time)
            .Select(l => new LoadDto(l.Time, l.SensorName, l.Load))
            .ToListAsync(ct);

        return Ok(readings);
    }

    [HttpGet("{raceNumber:int}/shift-angles")]
    public async Task<ActionResult<List<ShiftAngleDto>>> GetShiftAngles(
        int sessionId, int raceNumber, [FromQuery] double? from, [FromQuery] double? to, CancellationToken ct)
    {
        var race = await FindRaceAsync(sessionId, raceNumber, ct);
        if (race is null) return NotFound();

        var (start, end) = ComputeTimeWindow(race, from, to);

        var readings = await db.ShiftAngles
            .Where(s => s.SessionId == sessionId && s.Time >= start && s.Time <= end)
            .OrderBy(s => s.Time)
            .Select(s => new ShiftAngleDto(s.Time, s.IsPort, s.IsManual, s.TrueHeading, s.SpeedOverGround))
            .ToListAsync(ct);

        return Ok(readings);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    [HttpPatch("{raceNumber:int}")]
    public async Task<ActionResult<RaceDto>> Patch(int sessionId, int raceNumber, PatchRaceRequest request, CancellationToken ct)
    {
        var race = await db.Races
            .Include(r => r.Course)
            .FirstOrDefaultAsync(r => r.SessionId == sessionId && r.RaceNumber == raceNumber, ct);

        if (race is null) return NotFound();

        if (request.CourseId.HasValue)
            race.CourseId = request.CourseId.Value == 0 ? null : request.CourseId.Value;

        await db.SaveChangesAsync(ct);

        await db.Entry(race).Reference(r => r.Course).LoadAsync(ct);

        return Ok(new RaceDto(
            race.RaceNumber,
            race.CourseId,
            race.Course?.Name,
            race.StartedAt,
            race.EndedAt,
            (race.EndedAt - race.StartedAt).TotalSeconds,
            race.SailedDistanceMeters,
            race.MaxSpeedOverGround));
    }

    private async Task<Race?> FindRaceAsync(int sessionId, int raceNumber, CancellationToken ct)
    {
        return await db.Races
            .FirstOrDefaultAsync(r => r.SessionId == sessionId && r.RaceNumber == raceNumber, ct);
    }

    /// <summary>
    /// Computes the absolute time window for a race, applying optional second-offset filters.
    /// </summary>
    private static (DateTimeOffset Start, DateTimeOffset End) ComputeTimeWindow(Race race, double? fromSeconds, double? toSeconds)
    {
        var start = fromSeconds.HasValue
            ? race.StartedAt.AddSeconds(fromSeconds.Value)
            : race.StartedAt;

        var end = toSeconds.HasValue
            ? race.StartedAt.AddSeconds(toSeconds.Value)
            : race.EndedAt;

        return (start, end);
    }
}
