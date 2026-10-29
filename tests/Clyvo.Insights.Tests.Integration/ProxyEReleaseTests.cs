using System.Net;
using System.Reflection;
using System.Text.Json;
using Clyvo.Insights.Tests.Integration.Fixtures;

namespace Clyvo.Insights.Tests.Integration;

/// <summary>
/// Links atrás de proxy reverso e a versão que a API declara.
/// </summary>
[Collection(ColecaoDaApi.Nome)]
public class ProxyEReleaseTests
{
    private const string HostPublico = "insights.clyvo.example";

    private readonly FabricaDaApi _api;

    public ProxyEReleaseTests(FabricaDaApi api)
    {
        _api = api;
    }

    [Fact]
    public async Task ListarObrigacoes_AtrasDeProxyComTls_LinksSaemComOEsquemaEOHostEncaminhados()
    {
        // Arrange — o proxy terminou o TLS e repassa em http para o container
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/insights/obrigacoes?tamanho=10");
        requisicao.Headers.Add("X-Forwarded-For", "203.0.113.7");
        requisicao.Headers.Add("X-Forwarded-Proto", "https");
        requisicao.Headers.Add("X-Forwarded-Host", HostPublico);

        // Act
        var json = await EnviarAsync(cliente, requisicao);

        // Assert — os links da página e os de cada item
        var hrefs = Hrefs(json.GetProperty("_links"))
            .Concat(json.GetProperty("itens").EnumerateArray().SelectMany(i => Hrefs(i.GetProperty("_links"))))
            .ToList();

        Assert.NotEmpty(hrefs);
        Assert.All(hrefs, href => Assert.StartsWith($"https://{HostPublico}/api/insights/obrigacoes", href));
    }

    [Fact]
    public async Task ObterCoorte_AtrasDeProxy_LinksParaOsBracosUsamOHostEncaminhado()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/insights/coorte");
        requisicao.Headers.Add("X-Forwarded-Proto", "https");
        requisicao.Headers.Add("X-Forwarded-Host", HostPublico);

        // Act
        var json = await EnviarAsync(cliente, requisicao);

        // Assert
        var links = json.GetProperty("_links");
        Assert.Equal($"https://{HostPublico}/api/insights/coorte", Href(links, "self"));
        Assert.Equal($"https://{HostPublico}/api/insights/obrigacoes?grupo=controle", Href(links, "obrigacoesControle"));
    }

    [Fact]
    public async Task ListarObrigacoes_SemCabecalhosDeProxy_LinksUsamOHostDaRequisicao()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var json = await EnviarAsync(cliente, new HttpRequestMessage(HttpMethod.Get, "/api/insights/obrigacoes"));

        // Assert
        Assert.All(Hrefs(json.GetProperty("_links")),
            href => Assert.StartsWith("http://localhost/api/insights/obrigacoes", href));
    }

    [Fact]
    public async Task Swagger_DocumentoDaApi_DeclaraAVersaoDoAssembly()
    {
        // Arrange — a versão vem do Directory.Build.props
        var versao = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        var cliente = _api.CreateClient();

        // Act
        var json = await EnviarAsync(cliente, new HttpRequestMessage(HttpMethod.Get, "/swagger/v1/swagger.json"));

        // Assert — sem o "+<commit>" e sem cair no 1.0.0 padrão do SDK
        Assert.Equal(versao, json.GetProperty("info").GetProperty("version").GetString());
        Assert.Matches(@"^\d+\.\d+\.\d+$", versao);
        Assert.NotEqual("1.0.0", versao);
    }

    private static async Task<JsonElement> EnviarAsync(HttpClient cliente, HttpRequestMessage requisicao)
    {
        var resposta = await cliente.SendAsync(requisicao);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private static IEnumerable<string> Hrefs(JsonElement links) =>
        links.EnumerateObject().Select(l => l.Value.GetProperty("href").GetString()!);

    private static string Href(JsonElement links, string relacao) =>
        links.GetProperty(relacao).GetProperty("href").GetString()!;
}
