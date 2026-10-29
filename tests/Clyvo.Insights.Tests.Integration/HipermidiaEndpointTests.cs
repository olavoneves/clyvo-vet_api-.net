using System.Net;
using System.Text.Json;
using Clyvo.Insights.Tests.Integration.Fixtures;

namespace Clyvo.Insights.Tests.Integration;

/// <summary>
/// <c>_links</c> nos GET de leitura agregada, e o Swagger que os documenta.
/// </summary>
[Collection(ColecaoDaApi.Nome)]
public class HipermidiaEndpointTests
{
    private readonly FabricaDaApi _api;

    public HipermidiaEndpointTests(FabricaDaApi api)
    {
        _api = api;
    }

    [Fact]
    public async Task ObterCoorte_ComTokenValido_TemSelfELinkParaAsObrigacoesDeCadaBraco()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var json = await GetJsonAsync(cliente, "/api/insights/coorte");
        var links = json.GetProperty("_links");

        // Assert
        Assert.Equal("http://localhost/api/insights/coorte", Href(links, "self"));
        Assert.Equal("http://localhost/api/insights/obrigacoes?grupo=tratamento", Href(links, "obrigacoesTratamento"));
        Assert.Equal("http://localhost/api/insights/obrigacoes?grupo=controle", Href(links, "obrigacoesControle"));

        // O link é navegável e devolve só o braço pedido
        var controle = await GetJsonAsync(cliente, Href(links, "obrigacoesControle"));
        Assert.All(controle.GetProperty("itens").EnumerateArray(),
            i => Assert.Equal("CONTROLE", i.GetProperty("grupo").GetString()));
    }

    [Fact]
    public async Task ObterResumoObrigacoes_ComTokenValido_TemUmLinkPorBaldeFiltrandoPeloStatus()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var json = await GetJsonAsync(cliente, "/api/insights/obrigacoes/resumo");
        var links = json.GetProperty("_links");

        // Assert
        Assert.Equal("http://localhost/api/insights/obrigacoes/resumo", Href(links, "self"));

        var esperados = new Dictionary<string, string>
        {
            ["cumpridas"] = "CUMPRIDA",
            ["pararamEmAgendada"] = "AGENDADA",
            ["pararamEmRespondida"] = "RESPONDIDA",
            ["pararamEmNotificada"] = "NOTIFICADA",
            ["perdidas"] = "PERDIDA",
            ["naoTrabalhadasPrevistas"] = "PREVISTA",
            ["naoTrabalhadasCanceladas"] = "CANCELADA"
        };

        foreach (var (relacao, status) in esperados)
        {
            Assert.StartsWith("http://localhost/api/insights/obrigacoes?", Href(links, relacao));
            Assert.Contains($"status={status}", Href(links, relacao));
            Assert.Contains("ate=", Href(links, relacao));
        }
    }

    [Fact]
    public async Task Swagger_DocumentoDaApi_DescreveAListagemComParametrosEmCamelCaseELinks()
    {
        // Arrange
        var cliente = _api.CreateClient();

        // Act
        var json = await GetJsonAsync(cliente, "/swagger/v1/swagger.json");

        // Assert
        var listagem = json.GetProperty("paths").GetProperty("/api/insights/obrigacoes").GetProperty("get");
        var parametros = listagem.GetProperty("parameters").EnumerateArray()
            .Select(p => p.GetProperty("name").GetString())
            .ToList();

        foreach (var esperado in new[]
                 { "pagina", "tamanho", "status", "grupo", "categoria", "de", "ate", "pet", "ordenarPor", "direcao" })
        {
            Assert.Contains(esperado, parametros);
        }

        Assert.True(json.GetProperty("paths").TryGetProperty("/api/insights/obrigacoes/{id}", out _));
        Assert.True(json.GetProperty("paths").TryGetProperty("/api/insights/metas/{id}", out var meta));
        Assert.True(meta.TryGetProperty("get", out _));

        // O recurso é documentado como DTO + _links, e não como { dados, links }
        var esquemas = json.GetProperty("components").GetProperty("schemas");
        var paginaDeObrigacoes = esquemas.EnumerateObject()
            .Single(e => e.Name.Contains("ObrigacaoDto", StringComparison.Ordinal)
                         && e.Name.Contains("Pagina", StringComparison.Ordinal));
        Assert.True(paginaDeObrigacoes.Value.GetProperty("properties").TryGetProperty("_links", out _));
    }

    [Theory]
    [InlineData("/api/insights/obrigacoes", "self", "next")]
    [InlineData("/api/insights/obrigacoes/{id}", "self")]
    [InlineData("/api/insights/obrigacoes/resumo", "self", "pararamEmNotificada")]
    [InlineData("/api/insights/coorte", "self", "obrigacoesControle")]
    [InlineData("/api/insights/metas", "self", "criar")]
    [InlineData("/api/insights/metas/{id}", "self", "atualizar", "remover")]
    public async Task Swagger_RespostaDeCadaGet_TemExemploComLinks(string caminho, params string[] relacoes)
    {
        // Arrange
        var cliente = _api.CreateClient();
        var json = await GetJsonAsync(cliente, "/swagger/v1/swagger.json");

        // Act — segue o $ref da resposta 200 até o schema em components
        var referencia = json.GetProperty("paths").GetProperty(caminho).GetProperty("get")
            .GetProperty("responses").GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()!;
        var esquema = json.GetProperty("components").GetProperty("schemas")
            .GetProperty(referencia.Split('/').Last());

        // Assert
        Assert.True(esquema.TryGetProperty("example", out var exemplo), $"{caminho} sem exemplo");
        var links = exemplo.GetProperty("_links");

        foreach (var relacao in relacoes)
        {
            Assert.StartsWith("http", Href(links, relacao));
        }
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient cliente, string url)
    {
        var resposta = await cliente.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private static string Href(JsonElement links, string relacao) =>
        links.GetProperty(relacao).GetProperty("href").GetString()!;
}
