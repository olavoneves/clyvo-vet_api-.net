using Clyvo.Insights.Application.Metas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clyvo.Insights.Api.Controllers;

/// <summary>
/// Metas que a clínica define para os indicadores da análise de coorte.
/// </summary>
/// <remarks>
/// Todas as operações são recortadas pelo tenant do token. Meta de outra
/// clínica não é encontrada — responde 404, a mesma resposta de meta
/// inexistente, para não confirmar que aquele id existe em algum lugar.
/// </remarks>
[ApiController]
[Route("api/insights/metas")]
[Produces("application/json")]
[Authorize]
public sealed class MetasController : ControllerBase
{
    private readonly CriarMetaIndicador _criar;
    private readonly ListarMetasIndicador _listar;
    private readonly AtualizarMetaIndicador _atualizar;
    private readonly RemoverMetaIndicador _remover;

    public MetasController(
        CriarMetaIndicador criar,
        ListarMetasIndicador listar,
        AtualizarMetaIndicador atualizar,
        RemoverMetaIndicador remover)
    {
        _criar = criar;
        _listar = listar;
        _atualizar = atualizar;
        _remover = remover;
    }

    /// <summary>Lista as metas da clínica do token.</summary>
    /// <response code="200">Metas da clínica. Lista vazia quando não há nenhuma.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MetaIndicadorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<MetaIndicadorDto>>> Listar(
        CancellationToken cancellationToken)
    {
        var metas = await _listar.ExecutarAsync(cancellationToken);

        return Ok(metas);
    }

    /// <summary>Define uma meta para um indicador.</summary>
    /// <response code="201">Meta criada.</response>
    /// <response code="400">Indicador ausente ou limiar fora da faixa de 0 a 100.</response>
    /// <response code="409">A clínica já tem meta para esse indicador.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MetaIndicadorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MetaIndicadorDto>> Criar(
        [FromBody] CriarMetaIndicadorRequest requisicao,
        CancellationToken cancellationToken)
    {
        var meta = await _criar.ExecutarAsync(
            requisicao.Indicador!.Value,
            requisicao.LimiarPercentual!.Value,
            cancellationToken);

        return CreatedAtAction(nameof(Listar), new { id = meta.Id }, meta);
    }

    /// <summary>Move o piso de uma meta. É a única alteração que a meta aceita.</summary>
    /// <response code="200">Meta atualizada.</response>
    /// <response code="400">Limiar fora da faixa de 0 a 100.</response>
    /// <response code="404">Meta inexistente, ou de outra clínica.</response>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(MetaIndicadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MetaIndicadorDto>> Atualizar(
        long id,
        [FromBody] AtualizarMetaIndicadorRequest requisicao,
        CancellationToken cancellationToken)
    {
        var meta = await _atualizar.ExecutarAsync(
            id,
            requisicao.LimiarPercentual!.Value,
            cancellationToken);

        return Ok(meta);
    }

    /// <summary>Apaga uma meta.</summary>
    /// <response code="204">Meta removida.</response>
    /// <response code="404">Meta inexistente, ou de outra clínica.</response>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(long id, CancellationToken cancellationToken)
    {
        await _remover.ExecutarAsync(id, cancellationToken);

        return NoContent();
    }
}
