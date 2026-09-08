using System.Net;
using System.Net.Http.Json;
using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Tests.Integration.Fixtures;

namespace Clyvo.Insights.Tests.Integration;

/// <summary>
/// CRUD de metas pela borda HTTP.
/// </summary>
/// <remarks>
/// Cada teste cria a própria fábrica: as metas vivem num repositório em memória
/// compartilhado pelo host, e reaproveitá-lo entre testes faria o resultado
/// depender da ordem de execução.
/// </remarks>
public class MetasEndpointTests
{
    private const string Rota = "/api/insights/metas";

    [Fact]
    public async Task Sem_token_devolve_401()
    {
        // Arrange
        using var api = new FabricaDaApi();
        var cliente = api.CreateClient();

        // Act
        var resposta = await cliente.GetAsync(Rota);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Post_valido_devolve_201_com_a_meta_criada()
    {
        // Arrange
        using var api = new FabricaDaApi();
        var cliente = api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

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
    public async Task Post_invalido_devolve_400(string indicador, decimal limiar)
    {
        // Arrange
        using var api = new FabricaDaApi();
        var cliente = api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var resposta = await cliente.PostAsJsonAsync(
            Rota, new { indicador, limiarPercentual = limiar });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Post_do_mesmo_indicador_duas_vezes_devolve_409()
    {
        // Arrange
        using var api = new FabricaDaApi();
        var cliente = api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        var corpo = new { indicador = "TaxaCumprimento", limiarPercentual = 55m };
        await cliente.PostAsJsonAsync(Rota, corpo);

        // Act
        var resposta = await cliente.PostAsJsonAsync(Rota, corpo);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Put_devolve_200_com_o_limiar_novo()
    {
        // Arrange
        using var api = new FabricaDaApi();
        var cliente = api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
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
    public async Task Put_em_meta_inexistente_devolve_404()
    {
        // Arrange
        using var api = new FabricaDaApi();
        var cliente = api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var resposta = await cliente.PutAsJsonAsync($"{Rota}/999999", new { limiarPercentual = 62.5m });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Delete_devolve_204_e_a_meta_some_da_listagem()
    {
        // Arrange
        using var api = new FabricaDaApi();
        var cliente = api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        var criada = await CriarAsync(cliente, "TaxaCumprimento", 55m);

        // Act
        var resposta = await cliente.DeleteAsync($"{Rota}/{criada.Id}");
        var restantes = await cliente.GetFromJsonAsync<List<MetaIndicadorDto>>(Rota, FabricaDaApi.Json);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Empty(restantes!);
    }

    [Fact]
    public async Task Delete_de_meta_inexistente_devolve_404()
    {
        // Arrange
        using var api = new FabricaDaApi();
        var cliente = api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var resposta = await cliente.DeleteAsync($"{Rota}/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Meta_de_uma_clinica_nao_aparece_nem_e_alcancada_pela_outra()
    {
        // Arrange
        using var api = new FabricaDaApi();
        var daClinica = api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        var daVizinha = api.ClienteDaClinica(DadosCanonicos.ClinicaVizinha);
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
    public async Task Meta_definida_aparece_avaliada_na_analise_de_coorte()
    {
        // Arrange — a clínica cumpre 60% e a meta pede 75%
        using var api = new FabricaDaApi();
        var cliente = api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        await CriarAsync(cliente, "TaxaCumprimento", 75m);

        // Act
        var analise = await cliente.GetFromJsonAsync<Clyvo.Insights.Application.Coortes.AnaliseCoorteDto>(
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
