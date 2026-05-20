using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.net.Data;
using server.net.Models;

namespace server.net.Controllers;

/// <summary>Gerenciamento de consultas veterinárias</summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class ConsultasController(AppDbContext context) : ControllerBase
{
    /// <summary>Lista todas as consultas cadastradas</summary>
    /// <response code="200">Retorna a lista de consultas</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Consulta>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var consultas = await context.Consultas.ToListAsync();
        return Ok(consultas);
    }

    /// <summary>Busca uma consulta pelo ID</summary>
    /// <param name="id">ID da consulta</param>
    /// <response code="200">Consulta encontrada</response>
    /// <response code="404">Consulta não encontrada</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Consulta), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var consulta = await context.Consultas.FindAsync(id);
        if (consulta is null) return NotFound();
        return Ok(consulta);
    }

    /// <summary>Lista todas as consultas dos pets de um tutor</summary>
    /// <param name="tutorId">ID do tutor</param>
    /// <response code="200">Lista de consultas dos pets do tutor</response>
    /// <response code="404">Tutor não encontrado</response>
    [HttpGet("tutor/{tutorId:int}")]
    [ProducesResponseType(typeof(IEnumerable<Consulta>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByTutor(int tutorId)
    {
        var exists = await context.Tutores.AnyAsync(t => t.Id == tutorId);
        if (!exists) return NotFound();

        var consultas = await context.Consultas
            .Where(c => c.Pet!.TutorId == tutorId)
            .ToListAsync();

        return Ok(consultas);
    }

    /// <summary>Cadastra uma nova consulta</summary>
    /// <param name="consulta">Dados da consulta</param>
    /// <response code="201">Consulta criada com sucesso</response>
    /// <response code="400">Dados inválidos ou PetId inexistente</response>
    [HttpPost]
    [ProducesResponseType(typeof(Consulta), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(Consulta consulta)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var petExists = await context.Pets.AnyAsync(p => p.Id == consulta.PetId);
        if (!petExists) return BadRequest("PetId inválido.");

        context.Consultas.Add(consulta);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = consulta.Id }, consulta);
    }

    /// <summary>Atualiza os dados de uma consulta</summary>
    /// <param name="id">ID da consulta</param>
    /// <param name="consulta">Dados atualizados</param>
    /// <response code="204">Atualizado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="404">Consulta não encontrada</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>Remove uma consulta</summary>
    /// <param name="id">ID da consulta</param>
    /// <response code="204">Removido com sucesso</response>
    /// <response code="404">Consulta não encontrada</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var consulta = await context.Consultas.FindAsync(id);
        if (consulta is null) return NotFound();

        context.Consultas.Remove(consulta);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
