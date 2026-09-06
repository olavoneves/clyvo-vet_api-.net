using Clyvo.Insights.Application.Obrigacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clyvo.Insights.Api.Controllers;

/// <summary>Distribuição das obrigações clínicas por estado.</summary>
[ApiController]
[Route("api/insights/obrigacoes")]
[Produces("application/json")]
[Authorize]
public sealed class ObrigacoesController : ControllerBase
{
    private readonly ObterResumoObrigacoes _obterResumo;

    public ObrigacoesController(ObterResumoObrigacoes obterResumo)
    {
        _obterResumo = obterResumo;
    }

    /// <summary>
    /// Onde as obrigações vencidas da clínica pararam.
    /// </summary>
    /// <remarks>
    /// Baldes exclusivos: cada obrigação aparece uma vez só, no estado mais
    /// adiantado que alcançou. Obrigação futura não entra — ela ainda não teve
    /// a chance de virar comparecimento ou falta.
    /// </remarks>
    /// <response code="200">Resumo da clínica do token.</response>
    /// <response code="401">Token ausente, expirado ou com assinatura inválida.</response>
    [HttpGet("resumo")]
    [ProducesResponseType(typeof(ResumoObrigacoesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResumoObrigacoesDto>> Resumo(CancellationToken cancellationToken)
    {
        var resumo = await _obterResumo.ExecutarAsync(cancellationToken);

        return Ok(resumo);
    }
}
