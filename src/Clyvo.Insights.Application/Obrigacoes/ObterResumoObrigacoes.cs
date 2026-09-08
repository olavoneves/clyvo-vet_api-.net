using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Mapeamentos;

namespace Clyvo.Insights.Application.Obrigacoes;

/// <summary>
/// Distribuição das obrigações vencidas da clínica do tenant por estado final.
/// </summary>
public sealed class ObterResumoObrigacoes
{
    private readonly ICoorteReadRepository _repositorio;
    private readonly ITenantContext _tenant;

    public ObterResumoObrigacoes(ICoorteReadRepository repositorio, ITenantContext tenant)
    {
        _repositorio = repositorio;
        _tenant = tenant;
    }

    public async Task<ResumoObrigacoesDto> ExecutarAsync(CancellationToken cancellationToken = default)
    {
        var idClinica = _tenant.IdClinica;

        var funil = await _repositorio.ObterFunilDaClinicaAsync(idClinica, cancellationToken);

        return funil.ParaDominio().ParaDto(idClinica, funil);
    }
}
