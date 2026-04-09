using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Shared.Dtos.Stats;

namespace Vakaros.Vkx.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController(AppDbContext db) : ControllerBase
{
    [HttpGet("summary")]
    [EndpointSummary("Global statistics across all boats, sessions, and races.")]
    [EndpointDescription(
        "Returns aggregate totals for the entire dataset: session and race counts, " +
        "total sailed distance, top speed, and the date range of recorded sessions. " +
        "Intended as a high-level overview for dashboards and AI-generated insight reports.")]
    public async Task<ActionResult<GlobalStatsDto>> GetSummary(CancellationToken ct)
    {
        var totalBoats = await db.Boats.CountAsync(ct);

        var sessions = await db.Sessions.ToListAsync(ct);
        var races = await db.Races.ToListAsync(ct);

        var dto = new GlobalStatsDto(
            totalBoats,
            sessions.Count,
            races.Count,
            sessions.Count > 0 ? sessions.Sum(s => (s.EndedAt - s.StartedAt).TotalSeconds) : 0.0,
            races.Count > 0 ? races.Sum(r => (r.EndedAt - r.StartedAt).TotalSeconds) : 0.0,
            races.Count > 0 ? races.Sum(r => r.SailedDistanceMeters) : 0.0,
            races.Count > 0 ? races.Max(r => r.MaxSpeedOverGround) : 0f,
            sessions.Count > 0 ? sessions.Min(s => s.StartedAt) : null,
            sessions.Count > 0 ? sessions.Max(s => s.StartedAt) : null);

        return Ok(dto);
    }
}
