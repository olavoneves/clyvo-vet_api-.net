using System.Reflection;
using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Application.Obrigacoes;
using Clyvo.Insights.Application.Projecoes;
using Clyvo.Insights.Domain.Metas;
using Clyvo.Insights.Domain.Projecoes;

namespace Clyvo.Insights.Tests.Integration.Fixtures;

/// <summary>
/// Leitura das views servida de memória.
/// </summary>
/// <remarks>
/// O que estes testes exercitam é a fatia HTTP: rota, autenticação, recorte de
/// tenant, código de status e forma da resposta. Apontá-los para o Oracle
/// acrescentaria a checagem do SQL — que já é feita a cada execução manual
/// contra o container — ao custo de a suíte só rodar em máquina com o banco de
/// pé, e de o resultado depender de um seed que muda sozinho.
/// </remarks>
public sealed class CoorteReadRepositoryEmMemoria : ICoorteReadRepository
{
    private readonly Dictionary<long, IReadOnlyList<LinhaCoorte>> _coortes = new()
    {
        [DadosCanonicos.ClinicaComCoorte] = DadosCanonicos.CoortesDaClinicaComCoorte,
        [DadosCanonicos.ClinicaVizinha] = DadosCanonicos.CoortesDaVizinha
    };

    private readonly Dictionary<long, LinhaFunilObrigacoes> _funis = new()
    {
        [DadosCanonicos.ClinicaComCoorte] = DadosCanonicos.FunilDaClinicaComCoorte
    };

    public Task<IReadOnlyList<LinhaCoorte>> ObterCoortesDaClinicaAsync(
        long idClinica,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_coortes.TryGetValue(idClinica, out var linhas)
            ? linhas
            : Array.Empty<LinhaCoorte>());

    public Task<LinhaFunilObrigacoes> ObterFunilDaClinicaAsync(
        long idClinica,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_funis.TryGetValue(idClinica, out var funil)
            ? funil
            : DadosCanonicos.FunilVazio);
}

/// <summary>
/// Metas em memória, com o mesmo recorte de tenant do repositório real.
/// </summary>
/// <remarks>
/// O filtro por clínica é reproduzido aqui de propósito: sem ele, os testes de
/// isolamento entre tenants passariam por acidente da fixture em vez de provar
/// que o serviço filtra.
/// </remarks>
public sealed class MetaIndicadorRepositoryEmMemoria : IMetaIndicadorRepository
{
    private static readonly PropertyInfo PropriedadeId =
        typeof(MetaIndicador).GetProperty(nameof(MetaIndicador.Id))!;

    private readonly List<MetaIndicador> _metas = new();
    private readonly List<MetaIndicador> _pendentes = new();
    private long _proximoId = 1;

    public Task<IReadOnlyList<MetaIndicador>> ListarDaClinicaAsync(
        long idClinica,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MetaIndicador>>(
            _metas.Where(m => m.IdClinica == idClinica).OrderBy(m => m.Indicador).ToList());

    public Task<MetaIndicador?> ObterDaClinicaAsync(
        long idClinica,
        long idMeta,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_metas.FirstOrDefault(m => m.Id == idMeta && m.IdClinica == idClinica));

    public Task<bool> ExisteParaIndicadorAsync(
        long idClinica,
        IndicadorMonitorado indicador,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_metas.Any(m => m.IdClinica == idClinica && m.Indicador == indicador));

    public Task AdicionarAsync(MetaIndicador meta, CancellationToken cancellationToken = default)
    {
        _pendentes.Add(meta);

        return Task.CompletedTask;
    }

    public Task RemoverAsync(MetaIndicador meta, CancellationToken cancellationToken = default)
    {
        _metas.Remove(meta);

        return Task.CompletedTask;
    }

    /// <summary>Só aqui o id é atribuído, como faria o identity do Oracle.</summary>
    public Task SalvarAsync(CancellationToken cancellationToken = default)
    {
        foreach (var meta in _pendentes)
        {
            PropriedadeId.SetValue(meta, _proximoId++);
            _metas.Add(meta);
        }

        _pendentes.Clear();

        return Task.CompletedTask;
    }
}

/// <summary>Projeções em memória. Guarda o que foi gravado para inspeção.</summary>
public sealed class ProjecaoRepositoryEmMemoria : IProjecaoRepository
{
    private readonly Dictionary<(long, DateOnly), SnapshotCoorte> _snapshots = new();

    public List<RegistroDeConsulta> Consultas { get; } = new();

    public IReadOnlyCollection<SnapshotCoorte> Snapshots => _snapshots.Values;

    public Task<SnapshotCoorte?> ObterSnapshotDoDiaAsync(
        long idClinica,
        DateOnly dataReferencia,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_snapshots.GetValueOrDefault((idClinica, dataReferencia)));

    public Task RegistrarSnapshotAsync(
        SnapshotCoorte snapshot,
        AnaliseCoorteDto documento,
        CancellationToken cancellationToken = default)
    {
        _snapshots[(snapshot.IdClinica, snapshot.DataReferencia)] = snapshot;

        return Task.CompletedTask;
    }

    public Task RegistrarConsultaAsync(
        RegistroDeConsulta registro,
        CancellationToken cancellationToken = default)
    {
        Consultas.Add(registro);

        return Task.CompletedTask;
    }
}
