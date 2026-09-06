using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Application.Projecoes;
using Clyvo.Insights.Domain.Projecoes;
using Clyvo.Insights.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Clyvo.Insights.Infrastructure.Mongo;

/// <summary>
/// Escreve a série de apurações nos dois bancos: o cabeçalho numérico no
/// Oracle, o documento inteiro no Mongo.
/// </summary>
internal sealed class ProjecaoRepository : IProjecaoRepository
{
    private readonly InsightsDbContext _oracle;
    private readonly IMongoCollection<DocumentoSnapshotCoorte> _snapshots;
    private readonly IMongoCollection<DocumentoConsultaIndicador> _consultas;

    public ProjecaoRepository(
        InsightsDbContext oracle,
        IMongoCollection<DocumentoSnapshotCoorte> snapshots,
        IMongoCollection<DocumentoConsultaIndicador> consultas)
    {
        _oracle = oracle;
        _snapshots = snapshots;
        _consultas = consultas;
    }

    public Task<SnapshotCoorte?> ObterSnapshotDoDiaAsync(
        long idClinica,
        DateOnly dataReferencia,
        CancellationToken cancellationToken = default) =>
        _oracle.SnapshotsCoorte
            .FirstOrDefaultAsync(
                s => s.IdClinica == idClinica && s.DataReferencia == dataReferencia,
                cancellationToken);

    public async Task RegistrarSnapshotAsync(
        SnapshotCoorte snapshot,
        AnaliseCoorteDto documento,
        CancellationToken cancellationToken = default)
    {
        if (snapshot.Id == 0)
        {
            await _oracle.SnapshotsCoorte.AddAsync(snapshot, cancellationToken);
        }

        await _oracle.SaveChangesAsync(cancellationToken);

        var id = IdDoDocumento(snapshot.IdClinica, snapshot.DataReferencia);

        // Replace com upsert: reapurar o mesmo dia sobrescreve o documento em
        // vez de empilhar versões. A série é diária, não um histórico de
        // acessos.
        await _snapshots.ReplaceOneAsync(
            d => d.Id == id,
            new DocumentoSnapshotCoorte
            {
                Id = id,
                IdClinica = snapshot.IdClinica,
                DataReferencia = snapshot.DataReferencia.ToString("yyyy-MM-dd"),
                ApuradoEmUtc = DateTime.UtcNow,
                Analise = documento
            },
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public Task RegistrarConsultaAsync(
        RegistroDeConsulta registro,
        CancellationToken cancellationToken = default) =>
        _consultas.InsertOneAsync(
            new DocumentoConsultaIndicador
            {
                Id = ObjectId.GenerateNewId(),
                IdClinica = registro.IdClinica,
                Indicador = registro.Indicador,
                InstanteUtc = registro.InstanteUtc,
                Disponivel = registro.Disponivel,
                TraceId = registro.TraceId
            },
            options: null,
            cancellationToken);

    private static string IdDoDocumento(long idClinica, DateOnly data) =>
        $"{idClinica}:{data:yyyy-MM-dd}";
}
