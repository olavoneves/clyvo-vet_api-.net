using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.net.Data;
using server.net.Models;

namespace server.net.Controllers;

/// <summary>Gerenciamento de pets</summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class PetsController(AppDbContext context) : ControllerBase
{
    /// <summary>Lista todos os pets cadastrados</summary>
    /// <response code="200">Retorna a lista de pets</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Pet>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var pets = await context.Pets.ToListAsync();
        return Ok(pets);
    }

    /// <summary>Busca um pet pelo ID</summary>
    /// <param name="id">ID do pet</param>
    /// <response code="200">Pet encontrado</response>
    /// <response code="404">Pet não encontrado</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Pet), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var pet = await context.Pets.FindAsync(id);
        if (pet is null) return NotFound();
        return Ok(pet);
    }

    /// <summary>Lista todas as consultas de um pet</summary>
    /// <param name="id">ID do pet</param>
    /// <response code="200">Lista de consultas do pet</response>
    /// <response code="404">Pet não encontrado</response>
    [HttpGet("{id:int}/consultas")]
    [ProducesResponseType(typeof(IEnumerable<Consulta>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConsultas(int id)
    {
        var exists = await context.Pets.AnyAsync(p => p.Id == id);
        if (!exists) return NotFound();

        var consultas = await context.Consultas
            .Where(c => c.PetId == id)
            .ToListAsync();

        return Ok(consultas);
    }

    /// <summary>Cadastra um novo pet</summary>
    /// <param name="pet">Dados do pet</param>
    /// <response code="201">Pet criado com sucesso</response>
    /// <response code="400">Dados inválidos ou TutorId inexistente</response>
    [HttpPost]
    [ProducesResponseType(typeof(Pet), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(Pet pet)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var tutorExists = await context.Tutores.AnyAsync(t => t.Id == pet.TutorId);
        if (!tutorExists) return BadRequest("TutorId inválido.");

        context.Pets.Add(pet);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = pet.Id }, pet);
    }

    /// <summary>Atualiza os dados de um pet</summary>
    /// <param name="id">ID do pet</param>
    /// <param name="pet">Dados atualizados</param>
    /// <response code="204">Atualizado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="404">Pet não encontrado</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>Remove um pet</summary>
    /// <param name="id">ID do pet</param>
    /// <response code="204">Removido com sucesso</response>
    /// <response code="404">Pet não encontrado</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var pet = await context.Pets.FindAsync(id);
        if (pet is null) return NotFound();

        context.Pets.Remove(pet);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
