using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Clyvo.Insights.Infrastructure.Persistencia;

/// <summary>
/// Constrói o contexto para as ferramentas do EF em tempo de design.
/// </summary>
/// <remarks>
/// Sem isto, <c>dotnet ef</c> tentaria subir o host da API só para descobrir o
/// contexto — e a API recusa a subir sem a chave JWT configurada, o que faria
/// gerar migration exigir um segredo que nada tem a ver com schema.
/// </remarks>
public sealed class InsightsDbContextFactory : IDesignTimeDbContextFactory<InsightsDbContext>
{
    private const string VariavelDeAmbiente = "ConnectionStrings__Oracle";

    public InsightsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(VariavelDeAmbiente);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Defina {VariavelDeAmbiente} antes de rodar as ferramentas do EF. " +
                "Ex.: ConnectionStrings__Oracle=\"User Id=petflow;Password=...;Data Source=localhost:1521/PETFLOWDB\"");
        }

        var construtor = new DbContextOptionsBuilder<InsightsDbContext>();
        construtor.ConfigurarOracle(connectionString);

        return new InsightsDbContext(construtor.Options);
    }
}
