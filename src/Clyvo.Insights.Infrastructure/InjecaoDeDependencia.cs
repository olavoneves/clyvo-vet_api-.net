using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Infrastructure.Persistencia;
using Clyvo.Insights.Infrastructure.Persistencia.Repositorios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clyvo.Insights.Infrastructure;

/// <summary>Registra o acesso a dados: Oracle via EF Core.</summary>
public static class InjecaoDeDependencia
{
    /// <summary>Nome da connection string em configuração.</summary>
    public const string ConnectionStringOracle = "Oracle";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<InsightsDbContext>(options =>
            options.ConfigurarOracle(configuration.GetConnectionString(ConnectionStringOracle)));

        services.AddScoped<ICoorteReadRepository, CoorteReadRepository>();
        services.AddScoped<IMetaIndicadorRepository, MetaIndicadorRepository>();

        return services;
    }
}
