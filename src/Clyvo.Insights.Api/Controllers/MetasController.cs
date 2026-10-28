using Clyvo.Insights.Api.Hipermidia;
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
///
/// Cada meta sai com <c>self</c>, <c>atualizar</c> (PUT) e <c>remover</c>
/// (DELETE); a coleção, com <c>self</c> e <c>criar</c> (POST).
/// </remarks>
[ApiController]
[Route("api/insights/metas")]
[Produces("application/json")]
[Authorize]
public sealed class MetasController : ControllerBase
{
    private readonly CriarMetaIndicador _criar;
    private readonly ListarMetasIndicador _listar;
    private readonly ObterMetaIndicador _obter;
    private readonly AtualizarMetaIndicador _atualizar;
    private readonly RemoverMetaIndicador _remover;
    private readonly GeradorDeLinks _links;

    public MetasController(
        CriarMetaIndicador criar,
        ListarMetasIndicador listar,
        ObterMetaIndicador obter,
        AtualizarMetaIndicador atualizar,
        RemoverMetaIndicador remover,
        GeradorDeLinks links)
    {
        _criar = criar;
        _listar = listar;
        _obter = obter;
        _atualizar = atualizar;
        _remover = remover;
        _links = links;
    }

    /// <summary>Lista as metas da clínica do token.</summary>
    /// <response code="200">
    /// Metas da clínica em <c>itens</c>, cada uma com os próprios links. Coleção
    /// vazia quando não há nenhuma.
    /// </response>
    [HttpGet(Name = NomesDeRota.ListarMetas)]
    [ProducesResponseType(typeof(ColecaoDeRecursos<MetaIndicadorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ColecaoDeRecursos<MetaIndicadorDto>>> Listar(
        CancellationToken cancellationToken)
    {
        var metas = await _listar.ExecutarAsync(cancellationToken);

        return Ok(_links.ParaMetas(metas));
    }

    /// <summary>Uma meta da clínica do token.</summary>
    /// <response code="200">A meta, com <c>_links</c>.</response>
    /// <response code="404">Meta inexistente, ou de outra clínica.</response>
    [HttpGet("{id:long}", Name = NomesDeRota.ObterMeta)]
    [ProducesResponseType(typeof(Recurso<MetaIndicadorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Recurso<MetaIndicadorDto>>> Obter(long id, CancellationToken cancellationToken)
    {
        var meta = await _obter.ExecutarAsync(id, cancellationToken);

        return Ok(_links.ParaMeta(meta));
    }

    /// <summary>Define uma meta para um indicador.</summary>
    /// <response code="201">Meta criada, com <c>_links</c>. <c>Location</c> aponta para ela.</response>
    /// <response code="400">Indicador ausente ou limiar fora da faixa de 0 a 100.</response>
    /// <response code="409">A clínica já tem meta para esse indicador.</response>
    [HttpPost(Name = NomesDeRota.CriarMeta)]
    [ProducesResponseType(typeof(Recurso<MetaIndicadorDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Recurso<MetaIndicadorDto>>> Criar(
        [FromBody] CriarMetaIndicadorRequest requisicao,
        CancellationToken cancellationToken)
    {
        var meta = await _criar.ExecutarAsync(
            requisicao.Indicador!.Value,
            requisicao.LimiarPercentual!.Value,
            cancellationToken);

        return CreatedAtRoute(NomesDeRota.ObterMeta, new { id = meta.Id }, _links.ParaMeta(meta));
    }

    /// <summary>Move o piso de uma meta. É a única alteração que a meta aceita.</summary>
    /// <response code="200">Meta atualizada, com <c>_links</c>.</response>
    /// <response code="400">Limiar fora da faixa de 0 a 100.</response>
    /// <response code="404">Meta inexistente, ou de outra clínica.</response>
    [HttpPut("{id:long}", Name = NomesDeRota.AtualizarMeta)]
    [ProducesResponseType(typeof(Recurso<MetaIndicadorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Recurso<MetaIndicadorDto>>> Atualizar(
        long id,
        [FromBody] AtualizarMetaIndicadorRequest requisicao,
        CancellationToken cancellationToken)
    {
        var meta = await _atualizar.ExecutarAsync(
            id,
            requisicao.LimiarPercentual!.Value,
            cancellationToken);

        return Ok(_links.ParaMeta(meta));
    }

    /// <summary>Apaga uma meta.</summary>
    /// <response code="204">Meta removida.</response>
    /// <response code="404">Meta inexistente, ou de outra clínica.</response>
    [HttpDelete("{id:long}", Name = NomesDeRota.RemoverMeta)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(long id, CancellationToken cancellationToken)
    {
        await _remover.ExecutarAsync(id, cancellationToken);

        return NoContent();
    }
}
