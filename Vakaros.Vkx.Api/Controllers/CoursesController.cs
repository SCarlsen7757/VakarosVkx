using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Shared.Dtos;
using Vakaros.Vkx.Api.Models.Entities;

namespace Vakaros.Vkx.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CourseSummaryDto>>> GetAll([FromQuery] int? year, CancellationToken ct)
    {
        var query = db.Courses.AsQueryable();
        if (year.HasValue)
            query = query.Where(c => c.Year == year.Value);

        var courses = await query
            .OrderBy(c => c.Year).ThenBy(c => c.Name)
            .Select(c => new CourseSummaryDto(c.Id, c.Name, c.Year, c.Description, c.CreatedAt, c.Legs.Count))
            .ToListAsync(ct);

        return Ok(courses);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseDto>> GetById(int id, CancellationToken ct)
    {
        var course = await db.Courses
            .Include(c => c.Legs.OrderBy(l => l.SortOrder))
                .ThenInclude(l => l.Mark)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (course is null) return NotFound();

        return Ok(MapToDto(course));
    }

    [HttpPost]
    public async Task<ActionResult<CourseDto>> Create(CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Name = request.Name,
            Year = request.Year,
            Description = request.Description,
        };

        for (var i = 0; i < request.Legs.Count; i++)
        {
            var leg = request.Legs[i];
            course.Legs.Add(new CourseLeg
            {
                MarkId = leg.MarkId,
                SortOrder = i + 1,
                LegName = leg.LegName,
            });
        }

        db.Courses.Add(course);
        await db.SaveChangesAsync(ct);

        // Reload with mark navigation properties.
        var created = await db.Courses
            .Include(c => c.Legs.OrderBy(l => l.SortOrder))
                .ThenInclude(l => l.Mark)
            .FirstAsync(c => c.Id == course.Id, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CourseDto>> Update(int id, UpdateCourseRequest request, CancellationToken ct)
    {
        var course = await db.Courses
            .Include(c => c.Legs)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (course is null) return NotFound();

        course.Name = request.Name;
        course.Year = request.Year;
        course.Description = request.Description;

        // Replace legs entirely.
        db.CourseLegs.RemoveRange(course.Legs);
        course.Legs.Clear();

        for (var i = 0; i < request.Legs.Count; i++)
        {
            var leg = request.Legs[i];
            course.Legs.Add(new CourseLeg
            {
                MarkId = leg.MarkId,
                SortOrder = i + 1,
                LegName = leg.LegName,
            });
        }

        await db.SaveChangesAsync(ct);

        // Reload with mark navigation.
        var updated = await db.Courses
            .Include(c => c.Legs.OrderBy(l => l.SortOrder))
                .ThenInclude(l => l.Mark)
            .FirstAsync(c => c.Id == course.Id, ct);

        return Ok(MapToDto(updated));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var course = await db.Courses.FindAsync([id], ct);
        if (course is null) return NotFound();

        db.Courses.Remove(course);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    private static CourseDto MapToDto(Course course) => new(
        course.Id,
        course.Name,
        course.Year,
        course.Description,
        course.CreatedAt,
        [.. course.Legs.OrderBy(l => l.SortOrder).Select(l => new CourseLegDto(
            l.Id, l.MarkId, l.Mark.Name, l.SortOrder, l.LegName, l.Mark.Latitude, l.Mark.Longitude))]
    );
}
