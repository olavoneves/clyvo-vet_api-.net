using Clyvo.Insights.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Clyvo.Insights.Infrastructure.Saude;

/// <summary>
/// Verifica que o Oracle responde e que a view de contrato ainda está lá.
/// </summary>
/// <remarks>
/// Um <c>SELECT 1</c> provaria só que a conexão abre. A leitura da view prova
/// também que o schema esperado existe e que o usuário tem permissão nele — que
/// são as duas formas pelas quais este serviço quebra sem o banco ter caído.
/// </remarks>
internal sealed class OracleHealthCheck : IHealthCheck
{
    private readonly InsightsDbContext _contexto;

    public OracleHealthCheck(InsightsDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var linhas = await _contexto.PainelCoorte.CountAsync(cancellationToken);

            return HealthCheckResult.Healthy(
                $"VW_CLV_PAINEL_COORTE respondeu com {linhas} linha(s).");
        }
        catch (Exception excecao)
        {
            return HealthCheckResult.Unhealthy("Oracle indisponível ou contrato de leitura ausente.", excecao);
        }
    }
}
