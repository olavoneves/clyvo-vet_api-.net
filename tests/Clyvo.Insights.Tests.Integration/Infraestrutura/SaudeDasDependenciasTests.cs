using System.Net;
using Clyvo.Insights.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Clyvo.Insights.Tests.Integration.Infraestrutura;

/// <summary>
/// Como cada sonda de prontidão reage à dependência fora do ar.
/// </summary>
/// <remarks>
/// Os checks são resolvidos do mesmo <c>AddInfrastructure</c> que a API usa e
/// executados pelo <see cref="HealthCheckService"/>, com o nome e a severidade
/// registrados lá. Nada aqui precisa de banco: o Oracle e o Mongo apontam para
/// portas sem servidor, e o core é um handler HTTP dublado.
/// </remarks>
public class SaudeDasDependenciasTests
{
    private const string OracleSemServidor = "User Id=sem_banco;Data Source=localhost:1/TESTES_SEM_BANCO";
    private const string MongoSemServidor = "mongodb://localhost:1/?serverSelectionTimeoutMS=200&connectTimeoutMS=200";

    private static ServiceProvider Provedor(string? baseUrlDoCore = null, HttpMessageHandler? core = null)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Oracle"] = OracleSemServidor,
                ["Mongo:ConnectionString"] = MongoSemServidor,
                ["Mongo:Database"] = "clyvo_insights_testes",
                ["Core:BaseUrl"] = baseUrlDoCore,
                ["Core:TimeoutSegundos"] = "1"
            })
            .Build();

        var servicos = new ServiceCollection().AddLogging().AddInfrastructure(configuracao);

        if (core is not null)
        {
            servicos.AddHttpClient("clyvo-core").ConfigurePrimaryHttpMessageHandler(() => core);
        }

        return servicos.BuildServiceProvider();
    }

    private static async Task<HealthReportEntry> SondarAsync(ServiceProvider provedor, string nome)
    {
        var relatorio = await provedor.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(registro => registro.Name == nome);

        return relatorio.Entries[nome];
    }

    [Fact]
    public async Task OracleHealthCheck_SemServidor_FicaUnhealthyPorqueSemOracleNaoHaLeitura()
    {
        // Arrange
        await using var provedor = Provedor();

        // Act
        var resultado = await SondarAsync(provedor, "oracle");

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, resultado.Status);
        Assert.NotNull(resultado.Exception);
    }

    [Fact]
    public async Task MongoHealthCheck_SemServidor_FicaDegradedPorqueSoAsProjecoesParam()
    {
        // Arrange
        await using var provedor = Provedor();

        // Act
        var resultado = await SondarAsync(provedor, "mongo");

        // Assert
        Assert.Equal(HealthStatus.Degraded, resultado.Status);
        Assert.Contains("projeções", resultado.Description);
    }

    [Fact]
    public async Task CoreHealthCheck_SemBaseUrl_FicaDegradedDizendoQueASondaEstaDesligada()
    {
        // Arrange
        await using var provedor = Provedor(baseUrlDoCore: null);

        // Act
        var resultado = await SondarAsync(provedor, "clyvo-core");

        // Assert
        Assert.Equal(HealthStatus.Degraded, resultado.Status);
        Assert.Contains("Core:BaseUrl", resultado.Description);
    }

    [Fact]
    public async Task CoreHealthCheck_ComCoreRespondendo200_FicaHealthy()
    {
        // Arrange
        var core = new CoreDublado(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        await using var provedor = Provedor("http://core.test", core);

        // Act
        var resultado = await SondarAsync(provedor, "clyvo-core");

        // Assert
        Assert.Equal(HealthStatus.Healthy, resultado.Status);
        Assert.Equal("http://core.test/actuator/health", core.UltimaUrl);
    }

    [Fact]
    public async Task CoreHealthCheck_ComCoreRespondendo503_FicaDegradedComOStatus()
    {
        // Arrange
        var core = new CoreDublado(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        await using var provedor = Provedor("http://core.test", core);

        // Act
        var resultado = await SondarAsync(provedor, "clyvo-core");

        // Assert
        Assert.Equal(HealthStatus.Degraded, resultado.Status);
        Assert.Contains("503", resultado.Description);
    }

    [Fact]
    public async Task CoreHealthCheck_ComCoreInalcancavel_FicaDegradedComAExcecao()
    {
        // Arrange
        var core = new CoreDublado(_ => throw new HttpRequestException("conexão recusada"));
        await using var provedor = Provedor("http://core.test", core);

        // Act
        var resultado = await SondarAsync(provedor, "clyvo-core");

        // Assert
        Assert.Equal(HealthStatus.Degraded, resultado.Status);
        Assert.IsType<HttpRequestException>(resultado.Exception);
    }

    [Fact]
    public async Task CoreHealthCheck_ComCoreSemResponderNoPrazo_FicaDegradedPorTimeout()
    {
        // Arrange — o handler só termina quando o check desiste
        var core = new CoreDublado(async ct =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        await using var provedor = Provedor("http://core.test", core);

        // Act
        var resultado = await SondarAsync(provedor, "clyvo-core");

        // Assert
        Assert.Equal(HealthStatus.Degraded, resultado.Status);
        Assert.Contains("não respondeu em 1s", resultado.Description);
    }

    private sealed class CoreDublado : HttpMessageHandler
    {
        private readonly Func<CancellationToken, Task<HttpResponseMessage>> _responder;

        public CoreDublado(Func<CancellationToken, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        public string? UltimaUrl { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            UltimaUrl = request.RequestUri?.ToString();

            return _responder(cancellationToken);
        }
    }
}
