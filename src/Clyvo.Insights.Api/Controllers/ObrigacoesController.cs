using Clyvo.Insights.Api.Hipermidia;
using Clyvo.Insights.Application.Obrigacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clyvo.Insights.Api.Controllers;

/// <summary>Obrigações clínicas da clínica do token: listagem, detalhe e resumo por estado.</summary>
[ApiController]
[Route("api/insights/obrigacoes")]
[Produces("application/json")]
[Authorize]
public sealed class ObrigacoesController : ControllerBase
{
    private readonly ObterResumoObrigacoes _obterResumo;
    private readonly ListarObrigacoes _listar;
    private readonly ObterObrigacao _obter;
    private readonly GeradorDeLinks _links;

    public ObrigacoesController(
        ObterResumoObrigacoes obterResumo,
        ListarObrigacoes listar,
        ObterObrigacao obter,
        GeradorDeLinks links)
    {
        _obterResumo = obterResumo;
        _listar = listar;
        _obter = obter;
        _links = links;
    }

    /// <summary>
    /// Lista as obrigações da clínica, paginadas, com filtros e ordenação.
    /// </summary>
    /// <remarks>
    /// Lê <c>VW_CLV_OBRIGACAO_DETALHE</c>. Filtro, ordenação e corte da página
    /// acontecem no banco, e <c>totalItens</c> conta a mesma consulta filtrada.
    ///
    /// Os links de página (<c>first</c>, <c>prev</c>, <c>next</c>, <c>last</c>)
    /// repetem os filtros e a ordenação; <c>prev</c> some na primeira página e
    /// <c>next</c> na última. Página além da última responde 200 com
    /// <c>itens</c> vazio e <c>prev</c> apontando para a última.
    ///
    /// A ordenação desempata por id da obrigação, então a mesma consulta corta
    /// sempre as mesmas páginas.
    /// </remarks>
    /// <response code="200">Página da listagem, com <c>_links</c>.</response>
    /// <response code="400">Parâmetro fora da faixa ou desconhecido; o corpo diz qual.</response>
    /// <response code="401">Token ausente, expirado ou com assinatura inválida.</response>
    [HttpGet(Name = NomesDeRota.ListarObrigacoes)]
    [ProducesResponseType(typeof(PaginaDeRecursos<ObrigacaoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginaDeRecursos<ObrigacaoDto>>> Listar(
        [FromQuery] ConsultaObrigacoesRequest consulta,
        CancellationToken cancellationToken)
    {
        var listagem = await _listar.ExecutarAsync(consulta, cancellationToken);

        return Ok(_links.ParaListagemDeObrigacoes(listagem));
    }

    /// <summary>Uma obrigação da clínica do token.</summary>
    /// <response code="200">A obrigação, com <c>_links</c>.</response>
    /// <response code="401">Token ausente, expirado ou com assinatura inválida.</response>
    /// <response code="404">Obrigação inexistente, ou de outra clínica.</response>
    [HttpGet("{id:long}", Name = NomesDeRota.ObterObrigacao)]
    [ProducesResponseType(typeof(Recurso<ObrigacaoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Recurso<ObrigacaoDto>>> Obter(long id, CancellationToken cancellationToken)
    {
        var obrigacao = await _obter.ExecutarAsync(id, cancellationToken);

        return Ok(_links.ParaObrigacao(obrigacao));
    }

    /// <summary>
    /// Onde as obrigações vencidas da clínica pararam.
    /// </summary>
    /// <remarks>
    /// Baldes exclusivos: cada obrigação aparece uma vez só, no estado mais
    /// adiantado que alcançou. Obrigação futura não entra — ela ainda não teve
    /// a chance de virar comparecimento ou falta.
    ///
    /// Cada balde tem um link, com o nome do campo, para a listagem filtrada
    /// pelo status correspondente e limitada às obrigações já vencidas.
    /// <c>naoTrabalhadas</c> soma PREVISTA e CANCELADA e por isso tem dois.
    /// </remarks>
    /// <response code="200">Resumo da clínica do token, com <c>_links</c>.</response>
    /// <response code="401">Token ausente, expirado ou com assinatura inválida.</response>
    [HttpGet("resumo", Name = NomesDeRota.ObterResumoObrigacoes)]
    [ProducesResponseType(typeof(Recurso<ResumoObrigacoesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Recurso<ResumoObrigacoesDto>>> Resumo(CancellationToken cancellationToken)
    {
        var resumo = await _obterResumo.ExecutarAsync(cancellationToken);

        return Ok(_links.ParaResumoDeObrigacoes(resumo));
    }
}
