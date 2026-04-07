using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Shared.Dtos;
using Vakaros.Vkx.Api.Models.Entities;

namespace Vakaros.Vkx.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarksController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MarkDto>>> GetAll([FromQuery] int? year, CancellationToken ct)
    {
        var query = db.Marks.AsQueryable();
        if (year.HasValue)
            query = query.Where(m => m.Year == year.Value);

        var marks = await query
            .OrderBy(m => m.Year).ThenBy(m => m.Name)
            .Select(m => new MarkDto(m.Id, m.Name, m.Year, m.Latitude, m.Longitude, m.Description))
            .ToListAsync(ct);

        return Ok(marks);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MarkDto>> GetById(int id, CancellationToken ct)
    {
        var mark = await db.Marks.FindAsync([id], ct);
        if (mark is null) return NotFound();

        return Ok(new MarkDto(mark.Id, mark.Name, mark.Year, mark.Latitude, mark.Longitude, mark.Description));
    }

    [HttpPost]
    public async Task<ActionResult<MarkDto>> Create(CreateMarkRequest request, CancellationToken ct)
    {
        var mark = new Mark
        {
            Name = request.Name,
            Year = request.Year,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Description = request.Description,
        };

        db.Marks.Add(mark);
        await db.SaveChangesAsync(ct);

        var dto = new MarkDto(mark.Id, mark.Name, mark.Year, mark.Latitude, mark.Longitude, mark.Description);
        return CreatedAtAction(nameof(GetById), new { id = mark.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MarkDto>> Update(int id, UpdateMarkRequest request, CancellationToken ct)
    {
        var mark = await db.Marks.FindAsync([id], ct);
        if (mark is null) return NotFound();

        mark.Name = request.Name;
        mark.Year = request.Year;
        mark.Latitude = request.Latitude;
        mark.Longitude = request.Longitude;
        mark.Description = request.Description;

        await db.SaveChangesAsync(ct);

        return Ok(new MarkDto(mark.Id, mark.Name, mark.Year, mark.Latitude, mark.Longitude, mark.Description));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var mark = await db.Marks.FindAsync([id], ct);
        if (mark is null) return NotFound();

        // Reject deletion if referenced by any course leg.
        var isReferenced = await db.CourseLegs.AnyAsync(cl => cl.MarkId == id, ct);
        if (isReferenced)
            return Conflict(new { message = "Cannot delete mark; it is referenced by one or more course legs." });

        db.Marks.Remove(mark);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}
