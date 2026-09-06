using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Mapeamentos;
using Clyvo.Insights.Domain.Coortes;

namespace Clyvo.Insights.Application.Coortes;

/// <summary>
/// Busca as coortes da clínica do tenant, chama o domínio e devolve o DTO.
/// </summary>
/// <remarks>
/// Caso de uso é classe com um método público. Ele orquestra e mapeia; a
/// aritmética inteira mora no domínio, e recalculá-la aqui criaria uma segunda
/// implementação do mesmo número.
/// </remarks>
public sealed class ObterAnaliseCoorte
{
    private readonly ICoorteReadRepository _repositorio;
    private readonly ITenantContext _tenant;

    public ObterAnaliseCoorte(ICoorteReadRepository repositorio, ITenantContext tenant)
    {
        _repositorio = repositorio;
        _tenant = tenant;
    }

    /// <summary>Análise da clínica do token. Não recebe id de clínica de propósito.</summary>
    public async Task<AnaliseCoorteDto> ExecutarAsync(CancellationToken cancellationToken = default)
    {
        var idClinica = _tenant.IdClinica;

        var linhas = await _repositorio.ObterCoortesDaClinicaAsync(idClinica, cancellationToken);

        var analise = AnaliseCoorte.Calcular(
            linhas.ParaCoorteDoGrupo(GrupoCoorte.Tratado),
            linhas.ParaCoorteDoGrupo(GrupoCoorte.Controle),
            linhas.ParaTicketMedio());

        return analise.ParaDto(idClinica);
    }
}
