using Clyvo.Insights.Domain.Metas;

namespace Clyvo.Insights.Application.Abstracoes;

/// <summary>
/// Persistência das metas em <c>INS_META_INDICADOR</c>, a tabela que este
/// serviço é dono.
/// </summary>
/// <remarks>
/// Todo método recebe a clínica e filtra por ela. Não há sobrecarga que busque
/// por id sozinho: a que existisse seria usada uma vez sem o filtro, e aí uma
/// clínica editaria a meta de outra.
/// </remarks>
public interface IMetaIndicadorRepository
{
    Task<IReadOnlyList<MetaIndicador>> ListarDaClinicaAsync(
        long idClinica, CancellationToken cancellationToken = default);

    Task<MetaIndicador?> ObterDaClinicaAsync(
        long idClinica, long idMeta, CancellationToken cancellationToken = default);

    Task<bool> ExisteParaIndicadorAsync(
        long idClinica, IndicadorMonitorado indicador, CancellationToken cancellationToken = default);

    Task AdicionarAsync(MetaIndicador meta, CancellationToken cancellationToken = default);

    Task RemoverAsync(MetaIndicador meta, CancellationToken cancellationToken = default);

    /// <summary>Grava as alterações pendentes da unidade de trabalho.</summary>
    Task SalvarAsync(CancellationToken cancellationToken = default);
}
