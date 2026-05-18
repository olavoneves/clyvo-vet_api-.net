using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.net.Data;
using server.net.Models;

namespace server.net.Controllers;

[ApiController]
[Route("[controller]")]
public class ConsultasController(AppDbContext context) : ControllerBase
{
    // GET /consultas
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var consultas = await context.Consultas.ToListAsync();
        return Ok(consultas);
    }

    // GET /consultas/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var consulta = await context.Consultas.FindAsync(id);
        if (consulta is null) return NotFound();
        return Ok(consulta);
    }

    // GET /consultas/tutor/{tutorId}  — todas as consultas dos pets de um tutor
    [HttpGet("tutor/{tutorId:int}")]
    public async Task<IActionResult> GetByTutor(int tutorId)
    {
        var exists = await context.Tutores.AnyAsync(t => t.Id == tutorId);
        if (!exists) return NotFound();

        var consultas = await context.Consultas
            .Where(c => c.Pet.TutorId == tutorId)
            .ToListAsync();

        return Ok(consultas);
    }

    // POST /consultas
    [HttpPost]
    public async Task<IActionResult> Create(Consulta consulta)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var petExists = await context.Pets.AnyAsync(p => p.Id == consulta.PetId);
        if (!petExists) return BadRequest("PetId inválido.");

        context.Consultas.Add(consulta);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = consulta.Id }, consulta);
    }

    // PUT /consultas/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Consulta consulta)
    {
        if (id != consulta.Id) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var exists = await context.Consultas.AnyAsync(c => c.Id == id);
        if (!exists) return NotFound();

        context.Entry(consulta).State = EntityState.Modified;
        await context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE /consultas/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var consulta = await context.Consultas.FindAsync(id);
        if (consulta is null) return NotFound();

        context.Consultas.Remove(consulta);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
