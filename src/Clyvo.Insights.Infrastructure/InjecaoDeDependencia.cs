using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Infrastructure.Mongo;
using Clyvo.Insights.Infrastructure.Persistencia;
using Clyvo.Insights.Infrastructure.Persistencia.Repositorios;
using Clyvo.Insights.Infrastructure.Saude;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

        services.AddScoped<ILeituraDoCoreRepository, LeituraDoCoreRepository>();
        services.AddScoped<IMetaIndicadorRepository, MetaIndicadorRepository>();

        services.AddMongo(configuration);
        services.AddVerificacoesDeSaude(configuration);

        return services;
    }

    /// <summary>
    /// Registra as dependências externas no health check.
    /// </summary>
    /// <remarks>
    /// A tag <c>ready</c> separa quem responde por prontidão de quem responde
    /// por vida. <c>/health</c> não toca em banco nem em rede: se ele desse
    /// unhealthy por Oracle fora, o orquestrador reiniciaria um processo
    /// saudável em vez de esperar o banco voltar.
    ///
    /// Entre as dependências de prontidão, só o Oracle derruba o serviço. Mongo
    /// e clyvo-core degradam: o primeiro recebe apenas projeção de saída, e o
    /// segundo não está no caminho de nenhuma resposta — o token é validado
    /// localmente com a chave compartilhada.
    /// </remarks>
    private static IServiceCollection AddVerificacoesDeSaude(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var opcoesCore = configuration.GetSection(OpcoesCore.Secao).Get<OpcoesCore>() ?? new OpcoesCore();

        services.AddSingleton(opcoesCore);

        services.AddHttpClient(CoreHealthCheck.NomeDoCliente, cliente =>
        {
            if (!string.IsNullOrWhiteSpace(opcoesCore.BaseUrl))
            {
                cliente.BaseAddress = new Uri(opcoesCore.BaseUrl);
            }

            cliente.Timeout = TimeSpan.FromSeconds(opcoesCore.TimeoutSegundos);
        });

        services.AddHealthChecks()
            // Self só responde por "o processo está de pé e a pipeline responde".
            // É o que /health precisa: nenhuma dependência externa.
            .AddCheck("self", () => HealthCheckResult.Healthy("Processo respondendo."),
                tags: new[] { "live" })
            .AddCheck<OracleHealthCheck>(
                "oracle", HealthStatus.Unhealthy, tags: new[] { "ready", "db" })
            .AddCheck<MongoHealthCheck>(
                "mongo", HealthStatus.Degraded, tags: new[] { "ready", "db" })
            .AddCheck<CoreHealthCheck>(
                "clyvo-core", HealthStatus.Degraded, tags: new[] { "ready", "externo" });

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
