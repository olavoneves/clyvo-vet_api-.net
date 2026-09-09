using System.Diagnostics;
using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Mapeamentos;
using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Application.Projecoes;
using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Domain.Metas;
using Clyvo.Insights.Domain.Projecoes;
using Microsoft.Extensions.Logging;

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
    /// <summary>Rótulo do indicador no log de consulta.</summary>
    private const string IndicadorConsultado = "analise_coorte";

    private readonly ILeituraDoCoreRepository _coortes;
    private readonly IMetaIndicadorRepository _metas;
    private readonly IProjecaoRepository _projecoes;
    private readonly ITenantContext _tenant;
    private readonly ILogger<ObterAnaliseCoorte> _log;

    public ObterAnaliseCoorte(
        ILeituraDoCoreRepository coortes,
        IMetaIndicadorRepository metas,
        IProjecaoRepository projecoes,
        ITenantContext tenant,
        ILogger<ObterAnaliseCoorte> log)
    {
        _coortes = coortes;
        _metas = metas;
        _projecoes = projecoes;
        _tenant = tenant;
        _log = log;
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

        var dto = (analise.ParaDto(idClinica)) with
        {
            Metas = await AvaliarMetasAsync(idClinica, analise, cancellationToken)
        };

        await RegistrarProjecoesAsync(idClinica, analise, dto, cancellationToken);

        return dto;
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
        AnaliseCoorte analise,
        CancellationToken cancellationToken)
    {
        if (!analise.Disponivel)
        {
            return Array.Empty<MetaAvaliadaDto>();
        }

        var metas = await _metas.ListarDaClinicaAsync(idClinica, cancellationToken);

        return metas.Select(meta => meta.Avaliar(ValorApurado(meta.Indicador, analise))).ToList();
    }

    /// <summary>
    /// Grava o ponto do dia na série e registra a consulta no log.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A falha aqui é engolida com log de warning, e é a única exceção à regra
    /// de não capturar exceção fora do middleware. O motivo é que snapshot e
    /// log são escrita de saída: eles existem para responder perguntas
    /// posteriores, e derrubar a análise que o painel está pedindo agora porque
    /// o Mongo caiu troca uma perda de histórico por uma indisponibilidade.
    /// </para>
    /// <para>
    /// Nada do que é gravado aqui volta a ser lido para responder requisição.
    /// No momento em que voltar, o Mongo terá virado cache do Oracle.
    /// </para>
    /// </remarks>
    private async Task RegistrarProjecoesAsync(
        long idClinica,
        AnaliseCoorte analise,
        AnaliseCoorteDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            await _projecoes.RegistrarConsultaAsync(
                new RegistroDeConsulta(
                    idClinica,
                    IndicadorConsultado,
                    DateTime.UtcNow,
                    analise.Disponivel,
                    Activity.Current?.Id),
                cancellationToken);

            if (!analise.Disponivel)
            {
                return;
            }

            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

            var snapshot = await _projecoes.ObterSnapshotDoDiaAsync(idClinica, hoje, cancellationToken);

            if (snapshot is null)
            {
                snapshot = SnapshotCoorte.De(idClinica, analise, hoje);
            }
            else
            {
                snapshot.Reapurar(analise);
            }

            await _projecoes.RegistrarSnapshotAsync(snapshot, dto, cancellationToken);
        }
        catch (Exception excecao) when (excecao is not OperationCanceledException)
        {
            _log.LogWarning(excecao,
                "Não foi possível gravar as projeções da clínica {IdClinica}. " +
                "A análise foi respondida mesmo assim.", idClinica);
        }
    }

    private static decimal ValorApurado(IndicadorMonitorado indicador, AnaliseCoorte analise) => indicador switch
    {
        IndicadorMonitorado.TaxaCumprimento =>
            Math.Round(analise.Tratado.TaxaCumprimento * 100m, 2, MidpointRounding.AwayFromZero),

        IndicadorMonitorado.DeltaPontosPercentuais => analise.DeltaPontosPercentuais,

        _ => throw new InvalidOperationException(
            $"Indicador {indicador} não tem valor apurado na análise de coorte.")
    };
}
