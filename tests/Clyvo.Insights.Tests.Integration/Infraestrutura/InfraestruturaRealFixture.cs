using Clyvo.Insights.Infrastructure;
using Clyvo.Insights.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Clyvo.Insights.Tests.Integration.Infraestrutura;

/// <summary>
/// Conexões com a infraestrutura de verdade, compartilhadas pelo grupo.
/// </summary>
/// <remarks>
/// Abrir pool de Oracle e cliente de Mongo é caro e não deve acontecer por
/// classe de teste — daí a collection fixture. Quando o grupo está desabilitado,
/// a fixture não conecta em nada: ela é construída, mas os testes que a usariam
/// já foram ignorados pelo atributo.
/// </remarks>
public sealed class InfraestruturaRealFixture : IDisposable
{
    public InfraestruturaRealFixture()
    {
        ConnectionStringOracle = Environment.GetEnvironmentVariable("ConnectionStrings__Oracle") ?? string.Empty;
        ConnectionStringMongo = Environment.GetEnvironmentVariable("Mongo__ConnectionString") ?? string.Empty;
        NomeDoBancoMongo = Environment.GetEnvironmentVariable("Mongo__Database") ?? "clyvo_insights";
    }

    public string ConnectionStringOracle { get; }

    public string ConnectionStringMongo { get; }

    public string NomeDoBancoMongo { get; }

    /// <summary>
    /// Contexto novo a cada chamada: o <c>DbContext</c> não é thread-safe, e
    /// compartilhar um só entre testes da mesma collection trocaria o custo da
    /// conexão por falha intermitente.
    /// </summary>
    public InsightsDbContext CriarContexto()
    {
        Assert.False(string.IsNullOrWhiteSpace(ConnectionStringOracle),
            "ConnectionStrings__Oracle não definida. Veja RequerInfraestruturaAttribute.");

        var construtor = new DbContextOptionsBuilder<InsightsDbContext>();
        construtor.ConfigurarOracle(ConnectionStringOracle);

        return new InsightsDbContext(construtor.Options);
    }

    /// <summary>
    /// Monta o contêiner de DI chamando o mesmo <c>AddInfrastructure</c> que a
    /// API chama.
    /// </summary>
    /// <remarks>
    /// Resolver as coleções do Mongo daqui, e não construindo um MongoClient à
    /// mão, é o que faz estes testes cobrirem o registro de convenções — que é
    /// exatamente onde moravam os defeitos que dublê não pega: camelCase nos
    /// nomes de campo e Decimal128 no lugar de string.
    /// </remarks>
    public ServiceProvider CriarProvedor()
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Oracle"] = ConnectionStringOracle,
                ["Mongo:ConnectionString"] = ConnectionStringMongo,
                ["Mongo:Database"] = NomeDoBancoMongo
            })
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddInfrastructure(configuracao)
            .BuildServiceProvider();
    }

    public IMongoDatabase AbrirMongo()
    {
        Assert.False(string.IsNullOrWhiteSpace(ConnectionStringMongo),
            "Mongo__ConnectionString não definida. Veja RequerInfraestruturaAttribute.");

        return new MongoClient(ConnectionStringMongo).GetDatabase(NomeDoBancoMongo);
    }

    public void Dispose()
    {
    }
}

/// <summary>
/// Agrupa os testes que tocam infraestrutura real. Além de compartilhar a
/// fixture, a collection os serializa — eles escrevem em tabelas e coleções
/// compartilhadas, e rodá-los em paralelo produziria falha intermitente.
/// </summary>
[CollectionDefinition(Nome)]
public sealed class ColecaoDeInfraestruturaReal : ICollectionFixture<InfraestruturaRealFixture>
{
    public const string Nome = "Infraestrutura real";
}
