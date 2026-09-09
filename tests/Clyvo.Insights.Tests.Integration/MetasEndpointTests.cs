using System.Net;
using System.Net.Http.Json;
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
        var restantes = await cliente.GetFromJsonAsync<List<MetaIndicadorDto>>(Rota, FabricaDaApi.Json);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Empty(restantes!);
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
        var listaDaVizinha = await daVizinha.GetFromJsonAsync<List<MetaIndicadorDto>>(Rota, FabricaDaApi.Json);
        var putDaVizinha = await daVizinha.PutAsJsonAsync($"{Rota}/{criada.Id}", new { limiarPercentual = 10m });
        var deleteDaVizinha = await daVizinha.DeleteAsync($"{Rota}/{criada.Id}");

        // Assert — 404, e não 403: distinguir "não existe" de "é de outro
        // tenant" já confirmaria que aquele id existe em algum lugar.
        Assert.Empty(listaDaVizinha!);
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

    private static async Task<MetaIndicadorDto> CriarAsync(HttpClient cliente, string indicador, decimal limiar)
    {
        var resposta = await cliente.PostAsJsonAsync(
            "/api/insights/metas", new { indicador, limiarPercentual = limiar });

        resposta.EnsureSuccessStatusCode();

        return (await resposta.Content.ReadFromJsonAsync<MetaIndicadorDto>(FabricaDaApi.Json))!;
    }
}
