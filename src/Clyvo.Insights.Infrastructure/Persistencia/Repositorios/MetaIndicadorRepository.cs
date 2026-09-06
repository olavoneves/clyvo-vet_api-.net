using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Domain.Metas;
using Microsoft.EntityFrameworkCore;

namespace Clyvo.Insights.Infrastructure.Persistencia.Repositorios;

internal sealed class MetaIndicadorRepository : IMetaIndicadorRepository
{
    private readonly InsightsDbContext _contexto;

    public MetaIndicadorRepository(InsightsDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<IReadOnlyList<MetaIndicador>> ListarDaClinicaAsync(
        long idClinica,
        CancellationToken cancellationToken = default) =>
        await _contexto.Metas
            .Where(m => m.IdClinica == idClinica)
            .OrderBy(m => m.Indicador)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Busca por id <b>e</b> clínica. Meta de outro tenant não é encontrada, o
    /// que faz a API responder 404 — a mesma resposta de meta inexistente.
    /// </summary>
    public Task<MetaIndicador?> ObterDaClinicaAsync(
        long idClinica,
        long idMeta,
        CancellationToken cancellationToken = default) =>
        _contexto.Metas
            .FirstOrDefaultAsync(m => m.Id == idMeta && m.IdClinica == idClinica, cancellationToken);

    public Task<bool> ExisteParaIndicadorAsync(
        long idClinica,
        IndicadorMonitorado indicador,
        CancellationToken cancellationToken = default) =>
        _contexto.Metas
            .AnyAsync(m => m.IdClinica == idClinica && m.Indicador == indicador, cancellationToken);

    public async Task AdicionarAsync(MetaIndicador meta, CancellationToken cancellationToken = default) =>
        await _contexto.Metas.AddAsync(meta, cancellationToken);

    public Task RemoverAsync(MetaIndicador meta, CancellationToken cancellationToken = default)
    {
        _contexto.Metas.Remove(meta);

        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken = default) =>
        _contexto.SaveChangesAsync(cancellationToken);
}
