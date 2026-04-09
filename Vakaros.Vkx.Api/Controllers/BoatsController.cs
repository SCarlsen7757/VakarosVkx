using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Api.Models.Entities;
using Vakaros.Vkx.Shared.Dtos.Boats;

namespace Vakaros.Vkx.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BoatsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<BoatDto>>> GetAll(CancellationToken ct)
    {
        var boats = await db.Boats
            .OrderBy(b => b.Name)
            .Select(b => new BoatDto(b.Id, b.Name, b.SailNumber, b.BoatClass, b.Description, b.CreatedAt))
            .ToListAsync(ct);

        return Ok(boats);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BoatDto>> GetById(int id, CancellationToken ct)
    {
        var boat = await db.Boats.FindAsync([id], ct);
        if (boat is null) return NotFound();

        return Ok(new BoatDto(boat.Id, boat.Name, boat.SailNumber, boat.BoatClass, boat.Description, boat.CreatedAt));
    }

    [HttpPost]
    public async Task<ActionResult<BoatDto>> Create(CreateBoatRequest request, CancellationToken ct)
    {
        var boat = new Boat
        {
            Name = request.Name,
            SailNumber = request.SailNumber,
            BoatClass = request.BoatClass,
            Description = request.Description,
        };

        db.Boats.Add(boat);
        await db.SaveChangesAsync(ct);

        var dto = new BoatDto(boat.Id, boat.Name, boat.SailNumber, boat.BoatClass, boat.Description, boat.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = boat.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BoatDto>> Update(int id, UpdateBoatRequest request, CancellationToken ct)
    {
        var boat = await db.Boats.FindAsync([id], ct);
        if (boat is null) return NotFound();

        boat.Name = request.Name;
        boat.SailNumber = request.SailNumber;
        boat.BoatClass = request.BoatClass;
        boat.Description = request.Description;

        await db.SaveChangesAsync(ct);

        return Ok(new BoatDto(boat.Id, boat.Name, boat.SailNumber, boat.BoatClass, boat.Description, boat.CreatedAt));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var boat = await db.Boats.FindAsync([id], ct);
        if (boat is null) return NotFound();

        db.Boats.Remove(boat);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpGet("{id:int}/stats")]
    [EndpointSummary("Aggregate statistics for a boat across all its sessions and races.")]
    public async Task<ActionResult<BoatStatsDto>> GetStats(int id, CancellationToken ct)
    {
        var boat = await db.Boats.FindAsync([id], ct);
        if (boat is null) return NotFound();

        var sessions = await db.Sessions
            .Where(s => s.BoatId == id)
            .Include(s => s.Races)
            .ToListAsync(ct);

        var races = sessions.SelectMany(s => s.Races).ToList();

        var dto = new BoatStatsDto(
            boat.Id,
            boat.Name,
            boat.SailNumber,
            boat.BoatClass,
            sessions.Count,
            races.Count,
            sessions.Sum(s => (s.EndedAt - s.StartedAt).TotalSeconds),
            races.Sum(r => (r.EndedAt - r.StartedAt).TotalSeconds),
            races.Sum(r => r.SailedDistanceMeters),
            races.Count > 0 ? races.Max(r => r.MaxSpeedOverGround) : 0f);

        return Ok(dto);
    }
}
