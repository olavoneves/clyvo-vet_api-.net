using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.net.Data;
using server.net.Models;

namespace server.net.Controllers;

[ApiController]
[Route("[controller]")]
public class TutoresController(AppDbContext context) : ControllerBase
{
    // GET /tutores
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tutores = await context.Tutores.ToListAsync();
        return Ok(tutores);
    }

    // GET /tutores/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tutor = await context.Tutores.FindAsync(id);
        if (tutor is null) return NotFound();
        return Ok(tutor);
    }

    // GET /tutores/{id}/pets
    [HttpGet("{id:int}/pets")]
    public async Task<IActionResult> GetPets(int id)
    {
        var exists = await context.Tutores.AnyAsync(t => t.Id == id);
        if (!exists) return NotFound();

        var pets = await context.Pets
            .Where(p => p.TutorId == id)
            .ToListAsync();

        return Ok(pets);
    }

    // POST /tutores
    [HttpPost]
    public async Task<IActionResult> Create(Tutor tutor)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        context.Tutores.Add(tutor);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = tutor.Id }, tutor);
    }

    // PUT /tutores/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Tutor tutor)
    {
        if (id != tutor.Id) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var exists = await context.Tutores.AnyAsync(t => t.Id == id);
        if (!exists) return NotFound();

        context.Entry(tutor).State = EntityState.Modified;
        await context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE /tutores/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tutor = await context.Tutores.FindAsync(id);
        if (tutor is null) return NotFound();

        context.Tutores.Remove(tutor);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
