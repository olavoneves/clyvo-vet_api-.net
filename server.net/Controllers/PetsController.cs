using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.net.Data;
using server.net.Models;

namespace server.net.Controllers;

[ApiController]
[Route("[controller]")]
public class PetsController(AppDbContext context) : ControllerBase
{
    // GET /pets
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var pets = await context.Pets.ToListAsync();
        return Ok(pets);
    }

    // GET /pets/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var pet = await context.Pets.FindAsync(id);
        if (pet is null) return NotFound();
        return Ok(pet);
    }

    // GET /pets/{id}/consultas
    [HttpGet("{id:int}/consultas")]
    public async Task<IActionResult> GetConsultas(int id)
    {
        var exists = await context.Pets.AnyAsync(p => p.Id == id);
        if (!exists) return NotFound();

        var consultas = await context.Consultas
            .Where(c => c.PetId == id)
            .ToListAsync();

        return Ok(consultas);
    }

    // POST /pets
    [HttpPost]
    public async Task<IActionResult> Create(Pet pet)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var tutorExists = await context.Tutores.AnyAsync(t => t.Id == pet.TutorId);
        if (!tutorExists) return BadRequest("TutorId inválido.");

        context.Pets.Add(pet);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = pet.Id }, pet);
    }

    // PUT /pets/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Pet pet)
    {
        if (id != pet.Id) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var exists = await context.Pets.AnyAsync(p => p.Id == id);
        if (!exists) return NotFound();

        context.Entry(pet).State = EntityState.Modified;
        await context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE /pets/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var pet = await context.Pets.FindAsync(id);
        if (pet is null) return NotFound();

        context.Pets.Remove(pet);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
