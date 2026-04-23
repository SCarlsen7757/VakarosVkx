using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Auth;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Api.Helpers;
using Vakaros.Vkx.Api.Models.Entities;
using Vakaros.Vkx.Shared.Dtos.Courses;

namespace Vakaros.Vkx.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public class CoursesController(AppDbContext db, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CourseSummaryDto>>> GetAll([FromQuery] int? year, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var query = db.Courses.Where(c => c.OwnerUserId == userId);
        if (year.HasValue)
            query = query.Where(c => c.Year == year.Value);

        var courses = await query
            .OrderBy(c => c.Year).ThenBy(c => c.Name)
            .Select(c => new CourseSummaryDto(c.Id, c.Name, c.Year, c.Description, c.CreatedAt, c.Legs.Count))
            .ToListAsync(ct);
        return Ok(courses);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CourseDto>> GetById(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var course = await db.Courses
            .Where(c => c.Id == id && c.OwnerUserId == userId)
            .Include(c => c.Legs.OrderBy(l => l.SortOrder))
                .ThenInclude(l => l.Mark)
            .FirstOrDefaultAsync(ct);
        if (course is null) return NotFound();
        return Ok(MapToDto(course));
    }

    [HttpPost]
    public async Task<ActionResult<CourseDto>> Create(CreateCourseRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        // Ensure all referenced marks belong to the user.
        var markIds = request.Legs.Select(l => l.MarkId).Distinct().ToList();
        var ownedMarks = await db.Marks.CountAsync(m => markIds.Contains(m.Id) && m.OwnerUserId == userId, ct);
        if (ownedMarks != markIds.Count) return BadRequest(new { message = "One or more marks are not owned by you." });

        var course = new Course
        {
            OwnerUserId = userId,
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

        var created = await db.Courses
            .Include(c => c.Legs.OrderBy(l => l.SortOrder))
                .ThenInclude(l => l.Mark)
            .FirstAsync(c => c.Id == course.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CourseDto>> Update(Guid id, UpdateCourseRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var course = await db.Courses
            .Where(c => c.Id == id && c.OwnerUserId == userId)
            .Include(c => c.Legs)
            .FirstOrDefaultAsync(ct);
        if (course is null) return NotFound();

        var markIds = request.Legs.Select(l => l.MarkId).Distinct().ToList();
        var ownedMarks = await db.Marks.CountAsync(m => markIds.Contains(m.Id) && m.OwnerUserId == userId, ct);
        if (ownedMarks != markIds.Count) return BadRequest(new { message = "One or more marks are not owned by you." });

        course.Name = request.Name;
        course.Year = request.Year;
        course.Description = request.Description;
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

        var updated = await db.Courses
            .Include(c => c.Legs.OrderBy(l => l.SortOrder))
                .ThenInclude(l => l.Mark)
            .FirstAsync(c => c.Id == course.Id, ct);
        return Ok(MapToDto(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId, ct);
        if (course is null) return NotFound();
        db.Courses.Remove(course);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static CourseDto MapToDto(Course course)
    {
        var orderedLegs = course.Legs.OrderBy(l => l.SortOrder).ToList();
        var totalLengthMeters = 0.0;
        for (var i = 1; i < orderedLegs.Count; i++)
        {
            totalLengthMeters += GeoHelper.HaversineMeters(
                orderedLegs[i - 1].Mark.Latitude, orderedLegs[i - 1].Mark.Longitude,
                orderedLegs[i].Mark.Latitude, orderedLegs[i].Mark.Longitude);
        }
        return new CourseDto(
            course.Id, course.Name, course.Year, course.Description, course.CreatedAt, totalLengthMeters,
            [.. orderedLegs.Select(l => new CourseLegDto(
                l.Id, l.MarkId, l.Mark.Name, l.SortOrder, l.LegName, l.Mark.Latitude, l.Mark.Longitude))]
        );
    }
}
