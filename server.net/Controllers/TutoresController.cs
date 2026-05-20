using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.net.Data;
using server.net.Models;

namespace server.net.Controllers;

/// <summary>Gerenciamento de tutores</summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class TutoresController(AppDbContext context) : ControllerBase
{
    /// <summary>Lista todos os tutores cadastrados</summary>
    /// <response code="200">Retorna a lista de tutores</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Tutor>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var tutores = await context.Tutores.ToListAsync();
        return Ok(tutores);
    }

    /// <summary>Busca um tutor pelo ID</summary>
    /// <param name="id">ID do tutor</param>
    /// <response code="200">Tutor encontrado</response>
    /// <response code="404">Tutor não encontrado</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Tutor), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var tutor = await context.Tutores.FindAsync(id);
        if (tutor is null) return NotFound();
        return Ok(tutor);
    }

    /// <summary>Lista todos os pets de um tutor</summary>
    /// <param name="id">ID do tutor</param>
    /// <response code="200">Lista de pets do tutor</response>
    /// <response code="404">Tutor não encontrado</response>
    [HttpGet("{id:int}/pets")]
    [ProducesResponseType(typeof(IEnumerable<Pet>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPets(int id)
    {
        var exists = await context.Tutores.AnyAsync(t => t.Id == id);
        if (!exists) return NotFound();

        var pets = await context.Pets
            .Where(p => p.TutorId == id)
            .ToListAsync();

        return Ok(pets);
    }

    /// <summary>Cadastra um novo tutor</summary>
    /// <param name="tutor">Dados do tutor</param>
    /// <response code="201">Tutor criado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    [HttpPost]
    [ProducesResponseType(typeof(Tutor), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(Tutor tutor)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        context.Tutores.Add(tutor);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = tutor.Id }, tutor);
    }

    /// <summary>Atualiza os dados de um tutor</summary>
    /// <param name="id">ID do tutor</param>
    /// <param name="tutor">Dados atualizados</param>
    /// <response code="204">Atualizado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="404">Tutor não encontrado</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>Remove um tutor</summary>
    /// <param name="id">ID do tutor</param>
    /// <response code="204">Removido com sucesso</response>
    /// <response code="404">Tutor não encontrado</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var tutor = await context.Tutores.FindAsync(id);
        if (tutor is null) return NotFound();

        context.Tutores.Remove(tutor);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
