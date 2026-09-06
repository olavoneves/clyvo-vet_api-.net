using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Clyvo.Insights.Infrastructure.Saude;

/// <summary>Verifica que o MongoDB das projeções responde.</summary>
internal sealed class MongoHealthCheck : IHealthCheck
{
    private readonly IMongoDatabase _database;

    public MongoHealthCheck(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1), cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy($"MongoDB '{_database.DatabaseNamespace.DatabaseName}' respondeu ao ping.");
        }
        catch (Exception excecao)
        {
            // Degraded, e não Unhealthy: o Mongo só recebe projeção de saída. Com
            // ele fora a análise continua sendo respondida, e derrubar o
            // /health/ready tiraria de serviço uma instância que ainda serve.
            return HealthCheckResult.Degraded("MongoDB indisponível. As projeções não estão sendo gravadas.", excecao);
        }
    }
}
