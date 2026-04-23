using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Auth;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Api.Models.Entities;
using Vakaros.Vkx.Api.Services;
using Vakaros.Vkx.Shared.Dtos.Races;
using Vakaros.Vkx.Shared.Dtos.Telemetry;

namespace Vakaros.Vkx.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/sessions/{sessionId:guid}/races")]
public class RacesController(AppDbContext db, StartAnalysisService startAnalysis, SessionAuthorizer sessionAuth) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RaceDto>>> GetAll(Guid sessionId, CancellationToken ct)
    {
        if (!await sessionAuth.CanReadAsync(sessionId, ct)) return NotFound();

        var races = await db.Races
            .Where(r => r.SessionId == sessionId)
            .OrderBy(r => r.RaceNumber)
            .Select(r => new RaceDto(
                r.RaceNumber, r.CourseId, r.Course != null ? r.Course.Name : null,
                r.CountdownStartedAt, r.CountdownDurationSeconds,
                r.StartedAt, r.EndedAt,
                r.EndedAt.HasValue ? (r.EndedAt.Value - r.StartedAt).TotalSeconds : null,
                r.SailedDistanceMeters, r.MaxSpeedOverGround, r.Notes))
            .ToListAsync(ct);
        return Ok(races);
    }

    [HttpGet("{raceNumber:int}")]
    public async Task<ActionResult<RaceDetailDto>> GetByNumber(Guid sessionId, int raceNumber, CancellationToken ct)
    {
        if (!await sessionAuth.CanReadAsync(sessionId, ct)) return NotFound();
        var race = await FindRaceAsync(sessionId, raceNumber, ct);
        if (race is null) return NotFound();

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

        var duration = race.EndedAt.HasValue ? (race.EndedAt.Value - race.StartedAt).TotalSeconds : (double?)null;
        var startAnalysisResult = await startAnalysis.ComputeAsync(race, sessionId, pinEnd, boatEnd, ct);
        return Ok(new RaceDetailDto(race.RaceNumber, race.CourseId, race.CountdownStartedAt, race.CountdownDurationSeconds,
            race.StartedAt, race.EndedAt, duration, race.SailedDistanceMeters, race.MaxSpeedOverGround, race.Notes,
            pinEnd, boatEnd, startAnalysisResult));
    }

    [HttpGet("{raceNumber:int}/telemetry/positions")]
    public async Task<ActionResult<List<PositionDto>>> GetPositions(Guid sessionId, int raceNumber, [FromQuery] double? from, [FromQuery] double? to, CancellationToken ct)
    {
        if (!await sessionAuth.CanReadAsync(sessionId, ct)) return NotFound();
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

    [HttpGet("{raceNumber:int}/telemetry/wind")]
    public async Task<ActionResult<List<WindDto>>> GetWind(Guid sessionId, int raceNumber, [FromQuery] double? from, [FromQuery] double? to, CancellationToken ct)
    {
        if (!await sessionAuth.CanReadAsync(sessionId, ct)) return NotFound();
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

    [HttpGet("{raceNumber:int}/telemetry/speed-through-water")]
    public async Task<ActionResult<List<SpeedThroughWaterDto>>> GetSpeedThroughWater(Guid sessionId, int raceNumber, [FromQuery] double? from, [FromQuery] double? to, CancellationToken ct)
    {
        if (!await sessionAuth.CanReadAsync(sessionId, ct)) return NotFound();
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

    [HttpGet("{raceNumber:int}/telemetry/depth")]
    public async Task<ActionResult<List<DepthDto>>> GetDepth(Guid sessionId, int raceNumber, [FromQuery] double? from, [FromQuery] double? to, CancellationToken ct)
    {
        if (!await sessionAuth.CanReadAsync(sessionId, ct)) return NotFound();
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

    [HttpGet("{raceNumber:int}/telemetry/temperature")]
    public async Task<ActionResult<List<TemperatureDto>>> GetTemperature(Guid sessionId, int raceNumber, [FromQuery] double? from, [FromQuery] double? to, CancellationToken ct)
    {
        if (!await sessionAuth.CanReadAsync(sessionId, ct)) return NotFound();
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

    [HttpGet("{raceNumber:int}/telemetry/load")]
    public async Task<ActionResult<List<LoadDto>>> GetLoad(Guid sessionId, int raceNumber, [FromQuery] double? from, [FromQuery] double? to, CancellationToken ct)
    {
        if (!await sessionAuth.CanReadAsync(sessionId, ct)) return NotFound();
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

    [HttpGet("{raceNumber:int}/telemetry/shift-angles")]
    public async Task<ActionResult<List<ShiftAngleDto>>> GetShiftAngles(Guid sessionId, int raceNumber, [FromQuery] double? from, [FromQuery] double? to, CancellationToken ct)
    {
        if (!await sessionAuth.CanReadAsync(sessionId, ct)) return NotFound();
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

    [HttpGet("{raceNumber:int}/analysis/start-line-length")]
    public async Task<ActionResult<StartLineLengthDto>> GetStartLineLength(Guid sessionId, int raceNumber, CancellationToken ct)
    {
        if (!await sessionAuth.CanReadAsync(sessionId, ct)) return NotFound();
        var race = await FindRaceAsync(sessionId, raceNumber, ct);
        if (race is null) return NotFound();

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
        var result = StartAnalysisService.ComputeLineLength(pinEnd, boatEnd);
        if (result is null) return NoContent();
        return Ok(result);
    }

    [HttpPatch("{raceNumber:int}")]
    public async Task<ActionResult<RaceDto>> Patch(Guid sessionId, int raceNumber, PatchRaceRequest request, CancellationToken ct)
    {
        if (!await sessionAuth.CanWriteAsync(sessionId, ct)) return NotFound();
        var race = await db.Races
            .Include(r => r.Course)
            .FirstOrDefaultAsync(r => r.SessionId == sessionId && r.RaceNumber == raceNumber, ct);
        if (race is null) return NotFound();

        if (request.CourseId.HasValue)
            race.CourseId = request.CourseId.Value == Guid.Empty ? null : request.CourseId.Value;
        if (request.Notes is not null)
            race.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes;
        await db.SaveChangesAsync(ct);
        await db.Entry(race).Reference(r => r.Course).LoadAsync(ct);

        return Ok(new RaceDto(
            race.RaceNumber, race.CourseId, race.Course?.Name,
            race.CountdownStartedAt, race.CountdownDurationSeconds,
            race.StartedAt, race.EndedAt,
            race.EndedAt.HasValue ? (race.EndedAt.Value - race.StartedAt).TotalSeconds : null,
            race.SailedDistanceMeters, race.MaxSpeedOverGround, race.Notes));
    }

    private async Task<Race?> FindRaceAsync(Guid sessionId, int raceNumber, CancellationToken ct)
        => await db.Races.FirstOrDefaultAsync(r => r.SessionId == sessionId && r.RaceNumber == raceNumber, ct);

    private static (DateTimeOffset Start, DateTimeOffset End) ComputeTimeWindow(Race race, double? fromSeconds, double? toSeconds)
    {
        var start = fromSeconds.HasValue ? race.StartedAt.AddSeconds(fromSeconds.Value) : race.StartedAt;
        var end = toSeconds.HasValue ? race.StartedAt.AddSeconds(toSeconds.Value) : race.EndedAt ?? race.StartedAt;
        return (start, end);
    }
}
