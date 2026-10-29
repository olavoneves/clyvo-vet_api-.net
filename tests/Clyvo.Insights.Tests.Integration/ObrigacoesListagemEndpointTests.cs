using System.Net;
using System.Text.Json;
using Clyvo.Insights.Tests.Integration.Fixtures;

namespace Clyvo.Insights.Tests.Integration;

/// <summary>
/// Listagem paginada e detalhe de obrigações pela borda HTTP.
/// </summary>
/// <remarks>
/// As asserções leem o JSON cru, e não um DTO desserializado: o que está sendo
/// verificado é o contrato no fio — o nome <c>_links</c>, a ausência de
/// <c>prev</c> na primeira página —, e desserializar esconderia justamente isso.
/// </remarks>
[Collection(ColecaoDaApi.Nome)]
public class ObrigacoesListagemEndpointTests
{
    private const string Rota = "/api/insights/obrigacoes";

    private readonly FabricaDaApi _api;

    public ObrigacoesListagemEndpointTests(FabricaDaApi api)
    {
        _api = api;
    }

    [Fact]
    public async Task ListarObrigacoes_SemToken_Retorna401()
    {
        // Arrange
        var cliente = _api.CreateClient();

        // Act
        var resposta = await cliente.GetAsync(Rota);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task ListarObrigacoes_SemParametros_Retorna200ComEnvelopeEPrimeiraPagina()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var (status, json) = await GetJsonAsync(cliente, Rota);

        // Assert
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(1, json.GetProperty("pagina").GetInt32());
        Assert.Equal(20, json.GetProperty("tamanho").GetInt32());
        Assert.Equal(DadosCanonicos.TotalDeObrigacoesDaClinica, json.GetProperty("totalItens").GetInt32());
        Assert.Equal(3, json.GetProperty("totalPaginas").GetInt32());

        var itens = json.GetProperty("itens").EnumerateArray().ToList();
        Assert.Equal(20, itens.Count);

        // Ordenação padrão: data prevista crescente, que na fixture é o id.
        Assert.Equal(Enumerable.Range(1, 20).Select(i => (long)i), itens.Select(i => i.GetProperty("id").GetInt64()));

        var links = json.GetProperty("_links");
        Assert.True(links.TryGetProperty("self", out _));
        Assert.True(links.TryGetProperty("first", out _));
        Assert.True(links.TryGetProperty("next", out _));
        Assert.True(links.TryGetProperty("last", out _));
        Assert.False(links.TryGetProperty("prev", out _), "A primeira página não tem anterior.");
    }

    [Fact]
    public async Task ListarObrigacoes_CadaItem_TemSelfAbsolutoApontandoParaODetalhe()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var (_, json) = await GetJsonAsync(cliente, $"{Rota}?tamanho=1");
        var item = json.GetProperty("itens")[0];
        var self = Href(item.GetProperty("_links"), "self");

        // Assert — absoluto, com o host da requisição
        Assert.Equal($"http://localhost/api/insights/obrigacoes/{item.GetProperty("id").GetInt64()}", self);

        var (statusDoDetalhe, detalhe) = await GetJsonAsync(cliente, self);
        Assert.Equal(HttpStatusCode.OK, statusDoDetalhe);
        Assert.Equal(item.GetProperty("id").GetInt64(), detalhe.GetProperty("id").GetInt64());
        Assert.Equal(self, Href(detalhe.GetProperty("_links"), "self"));
    }

    [Fact]
    public async Task ListarObrigacoes_NaPaginaDoMeio_TemPrevENextPreservandoFiltroEOrdenacao()
    {
        // Arrange — 36 obrigações de tratamento (uma em cada cinco é controle),
        // em páginas de 10: quatro páginas, a 2 é do meio
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var (status, json) = await GetJsonAsync(
            cliente, $"{Rota}?grupo=tratamento&ordenarPor=valorEstimado&direcao=desc&tamanho=10&pagina=2");
        var links = json.GetProperty("_links");

        // Assert
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(36, json.GetProperty("totalItens").GetInt32());
        Assert.Equal(4, json.GetProperty("totalPaginas").GetInt32());

        foreach (var rel in new[] { "self", "first", "prev", "next", "last" })
        {
            var href = Href(links, rel);
            Assert.Contains("grupo=tratamento", href);
            Assert.Contains("ordenarPor=valorEstimado", href);
            Assert.Contains("direcao=desc", href);
            Assert.Contains("tamanho=10", href);
        }

        Assert.Contains("pagina=1", Href(links, "prev"));
        Assert.Contains("pagina=3", Href(links, "next"));
        Assert.Contains("pagina=4", Href(links, "last"));

        // E o next, seguido, devolve a página seguinte da mesma consulta
        var (_, proxima) = await GetJsonAsync(cliente, Href(links, "next"));
        Assert.Equal(3, proxima.GetProperty("pagina").GetInt32());
        Assert.Equal(36, proxima.GetProperty("totalItens").GetInt32());
    }

    [Fact]
    public async Task ListarObrigacoes_NaUltimaPagina_NaoTemNext()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var (_, json) = await GetJsonAsync(cliente, $"{Rota}?pagina=3");
        var links = json.GetProperty("_links");

        // Assert — 45 itens em páginas de 20: a terceira tem 5
        Assert.Equal(5, json.GetProperty("itens").GetArrayLength());
        Assert.False(links.TryGetProperty("next", out _), "A última página não tem próxima.");
        Assert.Contains("pagina=2", Href(links, "prev"));
        Assert.Equal(Href(links, "self"), Href(links, "last"));
    }

    [Fact]
    public async Task ListarObrigacoes_AlemDaUltimaPagina_Retorna200VazioComPrevNaUltima()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var (status, json) = await GetJsonAsync(cliente, $"{Rota}?pagina=50");
        var links = json.GetProperty("_links");

        // Assert
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(0, json.GetProperty("itens").GetArrayLength());
        Assert.Equal(DadosCanonicos.TotalDeObrigacoesDaClinica, json.GetProperty("totalItens").GetInt32());
        Assert.False(links.TryGetProperty("next", out _));
        Assert.Contains("pagina=3", Href(links, "prev"));
    }

    [Fact]
    public async Task ListarObrigacoes_ComFiltrosCombinados_DevolveSoOQueAtendeATodos()
    {
        // Arrange — CUMPRIDA são as obrigações 5, 12, 19, 26, 33, 40; dessas,
        // em janeiro (dias 1 a 31) ficam 5, 12, 19 e 26, e de controle só a 5
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var (status, json) = await GetJsonAsync(
            cliente, $"{Rota}?status=cumprida&de=2026-01-01&ate=2026-01-31&grupo=tratamento");

        // Assert
        Assert.Equal(HttpStatusCode.OK, status);

        var itens = json.GetProperty("itens").EnumerateArray().ToList();
        Assert.Equal(new long[] { 12, 19, 26 }, itens.Select(i => i.GetProperty("id").GetInt64()));
        Assert.All(itens, i =>
        {
            Assert.Equal("CUMPRIDA", i.GetProperty("status").GetString());
            Assert.Equal("TRATADO", i.GetProperty("grupo").GetString());
        });

        // O status volta canônico no link, mesmo pedido em minúsculas
        Assert.Contains("status=CUMPRIDA", Href(json.GetProperty("_links"), "self"));
    }

    [Theory]
    [InlineData("pagina=0", "pagina")]
    [InlineData("tamanho=0", "tamanho")]
    [InlineData("tamanho=101", "tamanho")]
    [InlineData("ordenarPor=nomeTutor", "ordenarPor")]
    [InlineData("direcao=cima", "direcao")]
    [InlineData("status=ATRASADA", "status")]
    [InlineData("grupo=tratado", "grupo")]
    [InlineData("de=2026-02-01&ate=2026-01-01", "de")]
    public async Task ListarObrigacoes_ComParametroInvalido_Retorna400ComOParametroNoProblemDetails(
        string query, string parametro)
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var resposta = await cliente.GetAsync($"{Rota}?{query}");
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty(parametro, out _),
            $"Esperado erro para '{parametro}' em: {json.RootElement}");
    }

    [Fact]
    public async Task ListarObrigacoes_ComTokenDaVizinha_SoVeAsObrigacoesDaVizinha()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaVizinha);

        // Act
        var (_, json) = await GetJsonAsync(cliente, Rota);

        // Assert
        Assert.Equal(2, json.GetProperty("totalItens").GetInt32());
        Assert.All(json.GetProperty("itens").EnumerateArray(),
            i => Assert.True(i.GetProperty("id").GetInt64() >= DadosCanonicos.ObrigacaoDaVizinha));
    }

    [Fact]
    public async Task ObterObrigacao_DeOutraClinica_Retorna404()
    {
        // Arrange — a obrigação existe, mas na vizinha
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var resposta = await cliente.GetAsync($"{Rota}/{DadosCanonicos.ObrigacaoDaVizinha}");

        // Assert — 404, e não 403: a mesma resposta de id inexistente
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task ObterObrigacao_SemToken_Retorna401()
    {
        // Arrange
        var cliente = _api.CreateClient();

        // Act
        var resposta = await cliente.GetAsync($"{Rota}/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    private static async Task<(HttpStatusCode Status, JsonElement Json)> GetJsonAsync(HttpClient cliente, string url)
    {
        var resposta = await cliente.GetAsync(url);
        var corpo = await resposta.Content.ReadAsStringAsync();

        return (resposta.StatusCode, JsonDocument.Parse(corpo).RootElement.Clone());
    }

    private static string Href(JsonElement links, string rel) =>
        links.GetProperty(rel).GetProperty("href").GetString()!;
}
