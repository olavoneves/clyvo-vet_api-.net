using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Application.Projecoes;
using Clyvo.Insights.Domain.Projecoes;

namespace Clyvo.Insights.Application.Abstracoes;

/// <summary>
/// Projeções derivadas da análise: a série de snapshots e o log de consulta.
/// </summary>
/// <remarks>
/// <para>
/// Nada aqui é lido para responder requisição. Snapshot e log são escrita de
/// saída — se alguma implementação passar a servir a resposta a partir deles, o
/// Mongo terá virado cache do Oracle, e os dois números vão divergir no dia em
/// que uma obrigação mudar de estado depois do último snapshot.
/// </para>
/// <para>
/// A escrita é dividida entre os dois bancos de propósito: o cabeçalho numérico
/// vai para <c>INS_SNAPSHOT_COORTE</c>, onde dá para juntar com o resto do
/// schema; o documento inteiro vai para o Mongo, porque a forma dele muda
/// conforme as metas que a clínica tenha definido, e versionar isso em coluna
/// relacional pediria migration a cada indicador novo.
/// </para>
/// </remarks>
public interface IProjecaoRepository
{
    /// <summary>Snapshot do dia da clínica, ou nulo se ainda não houver.</summary>
    Task<SnapshotCoorte?> ObterSnapshotDoDiaAsync(
        long idClinica, DateOnly dataReferencia, CancellationToken cancellationToken = default);

    /// <summary>
    /// Grava o snapshot do dia: cabeçalho no Oracle, documento no Mongo. Chamar
    /// duas vezes no mesmo dia corrige o ponto, não acrescenta outro.
    /// </summary>
    Task RegistrarSnapshotAsync(
        SnapshotCoorte snapshot, AnaliseCoorteDto documento, CancellationToken cancellationToken = default);

    /// <summary>Acrescenta uma linha ao log de consulta de indicadores.</summary>
    Task RegistrarConsultaAsync(
        RegistroDeConsulta registro, CancellationToken cancellationToken = default);
}
