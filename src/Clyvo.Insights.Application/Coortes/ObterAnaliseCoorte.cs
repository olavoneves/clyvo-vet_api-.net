using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Mapeamentos;
using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Domain.Metas;

namespace Clyvo.Insights.Application.Coortes;

/// <summary>
/// Busca as coortes da clínica do tenant, chama o domínio e devolve o DTO,
/// já confrontado com as metas que a clínica definiu.
/// </summary>
/// <remarks>
/// Caso de uso é classe com um método público. Ele orquestra e mapeia; a
/// aritmética inteira mora no domínio, e recalculá-la aqui criaria uma segunda
/// implementação do mesmo número.
/// </remarks>
public sealed class ObterAnaliseCoorte
{
    private readonly ICoorteReadRepository _coortes;
    private readonly IMetaIndicadorRepository _metas;
    private readonly ITenantContext _tenant;

    public ObterAnaliseCoorte(
        ICoorteReadRepository coortes,
        IMetaIndicadorRepository metas,
        ITenantContext tenant)
    {
        _coortes = coortes;
        _metas = metas;
        _tenant = tenant;
    }

    /// <summary>Análise da clínica do token. Não recebe id de clínica de propósito.</summary>
    public async Task<AnaliseCoorteDto> ExecutarAsync(CancellationToken cancellationToken = default)
    {
        var idClinica = _tenant.IdClinica;

        var linhas = await _coortes.ObterCoortesDaClinicaAsync(idClinica, cancellationToken);

        var analise = AnaliseCoorte.Calcular(
            linhas.ParaCoorteDoGrupo(GrupoCoorte.Tratado),
            linhas.ParaCoorteDoGrupo(GrupoCoorte.Controle),
            linhas.ParaTicketMedio());

        var dto = analise.ParaDto(idClinica);

        return dto with { Metas = await AvaliarMetasAsync(idClinica, dto, cancellationToken) };
    }

    /// <summary>
    /// Confronta cada meta com o valor apurado do seu indicador.
    /// </summary>
    /// <remarks>
    /// Análise indisponível não gera sinalização: os números vêm zerados por
    /// falta de contrafactual, e apontá-los como meta não atingida seria alarme
    /// falso justamente quando não se sabe nada.
    /// </remarks>
    private async Task<IReadOnlyList<MetaAvaliadaDto>> AvaliarMetasAsync(
        long idClinica,
        AnaliseCoorteDto dto,
        CancellationToken cancellationToken)
    {
        if (!dto.Disponivel)
        {
            return Array.Empty<MetaAvaliadaDto>();
        }

        var metas = await _metas.ListarDaClinicaAsync(idClinica, cancellationToken);

        return metas.Select(meta => meta.Avaliar(ValorApurado(meta.Indicador, dto))).ToList();
    }

    private static decimal ValorApurado(IndicadorMonitorado indicador, AnaliseCoorteDto dto) => indicador switch
    {
        IndicadorMonitorado.TaxaCumprimento => dto.Tratado.TaxaCumprimentoPercentual,
        IndicadorMonitorado.DeltaPontosPercentuais => dto.DeltaPontosPercentuais,
        _ => throw new InvalidOperationException(
            $"Indicador {indicador} não tem valor apurado na análise de coorte.")
    };
}
