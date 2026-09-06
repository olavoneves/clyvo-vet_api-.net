using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Infrastructure.Mongo;
using Clyvo.Insights.Infrastructure.Persistencia;
using Clyvo.Insights.Infrastructure.Persistencia.Repositorios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Clyvo.Insights.Infrastructure;

/// <summary>Registra o acesso a dados: Oracle via EF Core e MongoDB.</summary>
public static class InjecaoDeDependencia
{
    /// <summary>Nome da connection string em configuração.</summary>
    public const string ConnectionStringOracle = "Oracle";

    private const string ConvencaoCamelCase = "clyvo-insights";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<InsightsDbContext>(options =>
            options.ConfigurarOracle(configuration.GetConnectionString(ConnectionStringOracle)));

        services.AddScoped<ICoorteReadRepository, CoorteReadRepository>();
        services.AddScoped<IMetaIndicadorRepository, MetaIndicadorRepository>();

        services.AddMongo(configuration);

        return services;
    }

    private static IServiceCollection AddMongo(this IServiceCollection services, IConfiguration configuration)
    {
        var opcoes = configuration.GetSection(OpcoesMongo.Secao).Get<OpcoesMongo>() ?? new OpcoesMongo();

        // Campo no documento sai com a mesma grafia do JSON da API. Sem isto os
        // documentos nascem em PascalCase e quem consulta o Mongo tem de saber
        // que existem duas convenções para o mesmo campo.
        ConventionRegistry.Register(
            ConvencaoCamelCase,
            new ConventionPack { new CamelCaseElementNameConvention(), new ConvencaoDecimal128() },
            _ => true);

        services.AddSingleton(opcoes);

        // O IMongoClient é thread-safe e gerencia o próprio pool: uma instância
        // por processo. Criar um por requisição abriria um pool por requisição.
        services.AddSingleton<IMongoClient>(_ => new MongoClient(opcoes.ConnectionString));

        services.AddSingleton(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase(opcoes.Database));

        services.AddSingleton(sp => sp.GetRequiredService<IMongoDatabase>()
            .GetCollection<DocumentoSnapshotCoorte>(opcoes.ColecaoSnapshots));

        services.AddSingleton(sp => sp.GetRequiredService<IMongoDatabase>()
            .GetCollection<DocumentoConsultaIndicador>(opcoes.ColecaoConsultas));

        services.AddScoped<IProjecaoRepository, ProjecaoRepository>();

        return services;
    }
}
