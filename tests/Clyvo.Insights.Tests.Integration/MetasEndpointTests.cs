using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Clyvo.Insights.Api.Hipermidia;
using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Tests.Integration.Fixtures;

namespace Clyvo.Insights.Tests.Integration;

/// <summary>
/// CRUD de metas pela borda HTTP.
/// </summary>
/// <remarks>
/// Usa <c>IClassFixture</c>, e não a collection compartilhada: estes testes
/// criam e apagam metas, e um host mutável não pode ser dividido com as classes
/// que só leem. O host é reaproveitado entre os métodos desta classe — o xUnit
/// constrói uma instância nova da classe por teste, então o
/// <see cref="FabricaDaApi.LimparEstado"/> do construtor garante que cada um
/// comece do zero, independentemente da ordem de execução.
/// </remarks>
public class MetasEndpointTests : IClassFixture<FabricaDaApi>
{
    private const string Rota = "/api/insights/metas";

    private readonly FabricaDaApi _api;

    public MetasEndpointTests(FabricaDaApi api)
    {
        _api = api;
        _api.LimparEstado();
    }

    [Fact]
    public async Task ListarMetas_SemToken_Retorna401()
    {
        // Arrange
        var cliente = _api.CreateClient();

        // Act
        var resposta = await cliente.GetAsync(Rota);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarMeta_ComCorpoValido_Retorna201ComAMetaCriada()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var resposta = await cliente.PostAsJsonAsync(
            Rota, new { indicador = "TaxaCumprimento", limiarPercentual = 55m });
        var meta = await resposta.Content.ReadFromJsonAsync<MetaIndicadorDto>(FabricaDaApi.Json);

        // Assert
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(meta);
        Assert.True(meta!.Id > 0);
        Assert.Equal("TaxaCumprimento", meta.Indicador);
        Assert.Equal(55m, meta.LimiarPercentual);
    }

    [Theory]
    [InlineData("TaxaCumprimento", 150)]
    [InlineData("TaxaCumprimento", -1)]
    [InlineData("IndicadorQueNaoExiste", 55)]
    public async Task CriarMeta_ComCorpoInvalido_Retorna400(string indicador, decimal limiar)
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var resposta = await cliente.PostAsJsonAsync(
            Rota, new { indicador, limiarPercentual = limiar });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarMeta_ComIndicadorJaComMeta_Retorna409()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        var corpo = new { indicador = "TaxaCumprimento", limiarPercentual = 55m };
        await cliente.PostAsJsonAsync(Rota, corpo);

        // Act
        var resposta = await cliente.PostAsJsonAsync(Rota, corpo);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task AtualizarMeta_ComMetaExistente_Retorna200ComOLimiarNovo()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        var criada = await CriarAsync(cliente, "TaxaCumprimento", 55m);

        // Act
        var resposta = await cliente.PutAsJsonAsync($"{Rota}/{criada.Id}", new { limiarPercentual = 62.5m });
        var atualizada = await resposta.Content.ReadFromJsonAsync<MetaIndicadorDto>(FabricaDaApi.Json);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(62.5m, atualizada!.LimiarPercentual);
        Assert.Equal(criada.Id, atualizada.Id);
    }

    [Fact]
    public async Task AtualizarMeta_ComMetaInexistente_Retorna404()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var resposta = await cliente.PutAsJsonAsync($"{Rota}/999999", new { limiarPercentual = 62.5m });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task RemoverMeta_ComMetaExistente_Retorna204ESomeDaListagem()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        var criada = await CriarAsync(cliente, "TaxaCumprimento", 55m);

        // Act
        var resposta = await cliente.DeleteAsync($"{Rota}/{criada.Id}");
        var restantes = await cliente.GetFromJsonAsync<ColecaoDeRecursos<MetaIndicadorDto>>(Rota, FabricaDaApi.Json);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Empty(restantes!.Itens);
    }

    [Fact]
    public async Task RemoverMeta_ComMetaInexistente_Retorna404()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var resposta = await cliente.DeleteAsync($"{Rota}/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task ListarMetas_ComTokenDaClinicaVizinha_NaoAlcancaAMetaDaOutra()
    {
        // Arrange
        var daClinica = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        var daVizinha = _api.ClienteDaClinica(DadosCanonicos.ClinicaVizinha);
        var criada = await CriarAsync(daClinica, "TaxaCumprimento", 55m);

        // Act
        var listaDaVizinha = await daVizinha.GetFromJsonAsync<ColecaoDeRecursos<MetaIndicadorDto>>(Rota, FabricaDaApi.Json);
        var putDaVizinha = await daVizinha.PutAsJsonAsync($"{Rota}/{criada.Id}", new { limiarPercentual = 10m });
        var deleteDaVizinha = await daVizinha.DeleteAsync($"{Rota}/{criada.Id}");

        // Assert — 404, e não 403: distinguir "não existe" de "é de outro
        // tenant" já confirmaria que aquele id existe em algum lugar.
        Assert.Empty(listaDaVizinha!.Itens);
        Assert.Equal(HttpStatusCode.NotFound, putDaVizinha.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteDaVizinha.StatusCode);
    }

    [Fact]
    public async Task ObterCoorte_ComMetaDefinida_RetornaAMetaAvaliada()
    {
        // Arrange — a clínica cumpre 60% e a meta pede 75%
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        await CriarAsync(cliente, "TaxaCumprimento", 75m);

        // Act
        var analise = await cliente.GetFromJsonAsync<AnaliseCoorteDto>(
            "/api/insights/coorte", FabricaDaApi.Json);

        // Assert
        var avaliada = Assert.Single(analise!.Metas);
        Assert.Equal("TaxaCumprimento", avaliada.Indicador);
        Assert.Equal(75m, avaliada.LimiarPercentual);
        Assert.Equal(analise.Tratado.TaxaCumprimentoPercentual, avaliada.ValorApurado);
        Assert.True(avaliada.AbaixoDoLimiar);
    }

    [Fact]
    public async Task ListarMetas_ComMetas_TemLinksNaColecaoEEmCadaMetaComOMetodo()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        var criada = await CriarAsync(cliente, "TaxaCumprimento", 55m);

        // Act
        var resposta = await cliente.GetAsync(Rota);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;

        // Assert — a coleção
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var daColecao = raiz.GetProperty("_links");
        Assert.Equal("http://localhost/api/insights/metas", daColecao.GetProperty("self").GetProperty("href").GetString());
        Assert.Equal("http://localhost/api/insights/metas", daColecao.GetProperty("criar").GetProperty("href").GetString());
        Assert.Equal("POST", daColecao.GetProperty("criar").GetProperty("method").GetString());

        // e cada meta
        var daMeta = Assert.Single(raiz.GetProperty("itens").EnumerateArray()).GetProperty("_links");
        var urlDaMeta = $"http://localhost/api/insights/metas/{criada.Id}";
        Assert.Equal(urlDaMeta, daMeta.GetProperty("self").GetProperty("href").GetString());
        Assert.Equal(urlDaMeta, daMeta.GetProperty("atualizar").GetProperty("href").GetString());
        Assert.Equal("PUT", daMeta.GetProperty("atualizar").GetProperty("method").GetString());
        Assert.Equal(urlDaMeta, daMeta.GetProperty("remover").GetProperty("href").GetString());
        Assert.Equal("DELETE", daMeta.GetProperty("remover").GetProperty("method").GetString());
    }

    [Fact]
    public async Task CriarMeta_ComCorpoValido_DevolveLinksELocationApontandoParaAMeta()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var resposta = await cliente.PostAsJsonAsync(
            Rota, new { indicador = "DeltaPontosPercentuais", limiarPercentual = 15m });
        var recurso = await resposta.Content.ReadFromJsonAsync<Recurso<MetaIndicadorDto>>(FabricaDaApi.Json);

        // Assert
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal($"http://localhost/api/insights/metas/{recurso!.Dados.Id}", resposta.Headers.Location?.ToString());
        Assert.Equal(resposta.Headers.Location?.ToString(), recurso.Links["self"].Href);
        Assert.Equal("PUT", recurso.Links["atualizar"].Metodo);
    }

    [Fact]
    public async Task AtualizarMeta_ComMetaExistente_DevolveOsLinks()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        var criada = await CriarAsync(cliente, "TaxaCumprimento", 55m);

        // Act
        var resposta = await cliente.PutAsJsonAsync($"{Rota}/{criada.Id}", new { limiarPercentual = 60m });
        var recurso = await resposta.Content.ReadFromJsonAsync<Recurso<MetaIndicadorDto>>(FabricaDaApi.Json);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(60m, recurso!.Dados.LimiarPercentual);
        Assert.Equal(new[] { "atualizar", "remover", "self" }, recurso.Links.Keys.Order());
    }

    [Fact]
    public async Task ObterMeta_ComMetaExistente_Retorna200ComSelfApontandoParaSiMesma()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        var criada = await CriarAsync(cliente, "TaxaCumprimento", 55m);

        // Act
        var resposta = await cliente.GetAsync($"{Rota}/{criada.Id}");
        var recurso = await resposta.Content.ReadFromJsonAsync<Recurso<MetaIndicadorDto>>(FabricaDaApi.Json);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(criada.Id, recurso!.Dados.Id);
        Assert.Equal($"http://localhost/api/insights/metas/{criada.Id}", recurso.Links["self"].Href);
    }

    [Fact]
    public async Task ObterMeta_DeOutraClinica_Retorna404()
    {
        // Arrange
        var daClinica = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        var daVizinha = _api.ClienteDaClinica(DadosCanonicos.ClinicaVizinha);
        var criada = await CriarAsync(daClinica, "TaxaCumprimento", 55m);

        // Act
        var resposta = await daVizinha.GetAsync($"{Rota}/{criada.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    private static async Task<MetaIndicadorDto> CriarAsync(HttpClient cliente, string indicador, decimal limiar)
    {
        var resposta = await cliente.PostAsJsonAsync(
            "/api/insights/metas", new { indicador, limiarPercentual = limiar });

        resposta.EnsureSuccessStatusCode();

        return (await resposta.Content.ReadFromJsonAsync<MetaIndicadorDto>(FabricaDaApi.Json))!;
    }
}
