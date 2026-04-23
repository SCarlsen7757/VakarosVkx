using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Auth;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Api.Models.Entities;
using Vakaros.Vkx.Shared.Dtos.BoatClasses;

namespace Vakaros.Vkx.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public class BoatClassesController(AppDbContext db, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<BoatClassDto>>> GetAll(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var classes = await db.BoatClasses
            .Where(bc => bc.OwnerUserId == userId)
            .OrderBy(bc => bc.Name)
            .Select(bc => new BoatClassDto(bc.Id, bc.Name, bc.Length, bc.Width, bc.Weight))
            .ToListAsync(ct);
        return Ok(classes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BoatClassDto>> GetById(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var boatClass = await db.BoatClasses
            .Where(bc => bc.Id == id && bc.OwnerUserId == userId)
            .Select(bc => new BoatClassDto(bc.Id, bc.Name, bc.Length, bc.Width, bc.Weight))
            .FirstOrDefaultAsync(ct);
        if (boatClass is null) return NotFound();
        return Ok(boatClass);
    }

    [HttpPost]
    public async Task<ActionResult<BoatClassDto>> Create(CreateBoatClassRequest request, CancellationToken ct)
    {
        var boatClass = new BoatClass
        {
            OwnerUserId = currentUser.UserId,
            Name = request.Name,
            Length = request.Length,
            Width = request.Width,
            Weight = request.Weight,
        };
        db.BoatClasses.Add(boatClass);
        await db.SaveChangesAsync(ct);
        var dto = new BoatClassDto(boatClass.Id, boatClass.Name, boatClass.Length, boatClass.Width, boatClass.Weight);
        return CreatedAtAction(nameof(GetById), new { id = boatClass.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BoatClassDto>> Update(Guid id, UpdateBoatClassRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var boatClass = await db.BoatClasses.FirstOrDefaultAsync(bc => bc.Id == id && bc.OwnerUserId == userId, ct);
        if (boatClass is null) return NotFound();
        boatClass.Name = request.Name;
        boatClass.Length = request.Length;
        boatClass.Width = request.Width;
        boatClass.Weight = request.Weight;
        await db.SaveChangesAsync(ct);
        return Ok(new BoatClassDto(boatClass.Id, boatClass.Name, boatClass.Length, boatClass.Width, boatClass.Weight));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var boatClass = await db.BoatClasses.FirstOrDefaultAsync(bc => bc.Id == id && bc.OwnerUserId == userId, ct);
        if (boatClass is null) return NotFound();
        var isReferenced = await db.Boats.AnyAsync(b => b.BoatClassId == id, ct);
        if (isReferenced)
            return Conflict(new { message = "Cannot delete boat class; it is referenced by one or more boats." });
        db.BoatClasses.Remove(boatClass);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
