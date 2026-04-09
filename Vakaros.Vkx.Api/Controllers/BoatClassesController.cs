using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Api.Models.Entities;
using Vakaros.Vkx.Shared.Dtos.BoatClasses;

namespace Vakaros.Vkx.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BoatClassesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<BoatClassDto>>> GetAll(CancellationToken ct)
    {
        var classes = await db.BoatClasses
            .Include(bc => bc.Sails)
            .OrderBy(bc => bc.Name)
            .ToListAsync(ct);

        return Ok(classes.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BoatClassDto>> GetById(int id, CancellationToken ct)
    {
        var boatClass = await db.BoatClasses
            .Include(bc => bc.Sails)
            .FirstOrDefaultAsync(bc => bc.Id == id, ct);

        if (boatClass is null) return NotFound();
        return Ok(ToDto(boatClass));
    }

    [HttpPost]
    public async Task<ActionResult<BoatClassDto>> Create(CreateBoatClassRequest request, CancellationToken ct)
    {
        var boatClass = new BoatClass
        {
            Name = request.Name,
            LengthOverAll = request.LengthOverAll,
            Beam = request.Beam,
            Weight = request.Weight,
            BowspritLength = request.BowspritLength,
            Sails = request.Sails
                .Select(s => new Sail { Name = s.Name, Area = s.Area })
                .ToList(),
        };

        db.BoatClasses.Add(boatClass);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = boatClass.Id }, ToDto(boatClass));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BoatClassDto>> Update(int id, UpdateBoatClassRequest request, CancellationToken ct)
    {
        var boatClass = await db.BoatClasses
            .Include(bc => bc.Sails)
            .FirstOrDefaultAsync(bc => bc.Id == id, ct);

        if (boatClass is null) return NotFound();

        boatClass.Name = request.Name;
        boatClass.LengthOverAll = request.LengthOverAll;
        boatClass.Beam = request.Beam;
        boatClass.Weight = request.Weight;
        boatClass.BowspritLength = request.BowspritLength;

        db.Sails.RemoveRange(boatClass.Sails);
        boatClass.Sails = request.Sails
            .Select(s => new Sail { Name = s.Name, Area = s.Area })
            .ToList();

        await db.SaveChangesAsync(ct);

        return Ok(ToDto(boatClass));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var boatClass = await db.BoatClasses.FindAsync([id], ct);
        if (boatClass is null) return NotFound();

        db.BoatClasses.Remove(boatClass);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    private static BoatClassDto ToDto(BoatClass bc) => new(
        bc.Id,
        bc.Name,
        bc.LengthOverAll,
        bc.Beam,
        bc.Weight,
        bc.BowspritLength,
        bc.CreatedAt,
        bc.Sails.Select(s => new SailDto(s.Id, s.Name, s.Area)).ToList());
}
