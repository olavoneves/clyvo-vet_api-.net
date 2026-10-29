using Clyvo.Insights.Api.Hipermidia;
using Clyvo.Insights.Application.Coortes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clyvo.Insights.Api.Controllers;

/// <summary>
/// Análise de coorte: quanto da receita da clínica é atribuível ao produto.
/// </summary>
[ApiController]
[Route("api/insights/coorte")]
[Produces("application/json")]
public sealed class CoorteController : ControllerBase
{
    private readonly ObterAnaliseCoorte _obterAnaliseCoorte;
    private readonly GeradorDeLinks _links;

    public CoorteController(ObterAnaliseCoorte obterAnaliseCoorte, GeradorDeLinks links)
    {
        _obterAnaliseCoorte = obterAnaliseCoorte;
        _links = links;
    }

    /// <summary>
    /// Compara o grupo tratado com o grupo de controle e devolve o efeito
    /// atribuível ao produto.
    /// </summary>
    /// <remarks>
    /// Não recebe id de clínica: o tenant sai do claim <c>idClinica</c> do
    /// token emitido pelo clyvo-core. Um token de outra clínica devolve os
    /// dados daquela clínica, nunca os desta.
    ///
    /// Quando um dos braços ainda não tem obrigação resolvida, a resposta vem
    /// com <c>disponivel: false</c> e os números zerados — não é erro, é
    /// ausência de contrafactual.
    ///
    /// Os links <c>obrigacoesTratamento</c> e <c>obrigacoesControle</c> levam à
    /// listagem de obrigações filtrada por cada braço.
    /// </remarks>
    /// <response code="200">Análise da clínica do token, com <c>_links</c>.</response>
    /// <response code="401">Token ausente, expirado ou com assinatura inválida.</response>
    /// <response code="403">Token válido mas sem o claim de clínica.</response>
    [HttpGet(Name = NomesDeRota.ObterCoorte)]
    [Authorize]
    [ProducesResponseType(typeof(Recurso<AnaliseCoorteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Recurso<AnaliseCoorteDto>>> Obter(CancellationToken cancellationToken)
    {
        var analise = await _obterAnaliseCoorte.ExecutarAsync(cancellationToken);

        return Ok(_links.ParaAnaliseDeCoorte(analise));
    }
}
