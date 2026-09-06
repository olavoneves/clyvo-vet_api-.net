using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Clyvo.Insights.Application.Obrigacoes;
using Clyvo.Insights.Tests.Integration.Fixtures;

namespace Clyvo.Insights.Tests.Integration;

public class ObrigacoesEndpointTests : IClassFixture<FabricaDaApi>
{
    private const string Rota = "/api/insights/obrigacoes/resumo";

    private readonly FabricaDaApi _api;

    public ObrigacoesEndpointTests(FabricaDaApi api)
    {
        _api = api;
    }

    [Fact]
    public async Task Sem_token_devolve_401()
    {
        // Arrange
        var cliente = _api.CreateClient();

        // Act
        var resposta = await cliente.GetAsync(Rota);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Os_baldes_da_resposta_somam_o_total()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var resposta = await cliente.GetAsync(Rota);
        var resumo = await resposta.Content.ReadFromJsonAsync<ResumoObrigacoesDto>(FabricaDaApi.Json);

        // Assert — a promessa do endpoint é que cada obrigação apareça uma vez
        // só, e é isso que a soma verifica.
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.NotNull(resumo);

        var soma = resumo!.Cumpridas + resumo.PararamEmAgendada + resumo.PararamEmRespondida
                   + resumo.PararamEmNotificada + resumo.Perdidas + resumo.NaoTrabalhadas;

        Assert.Equal(resumo.Total, soma);
        Assert.Equal(DadosCanonicos.ClinicaComCoorte, resumo.IdClinica);
    }

    [Fact]
    public async Task Clinica_sem_obrigacao_vencida_devolve_200_zerado()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaSemDados);

        // Act
        var resposta = await cliente.GetAsync(Rota);
        var resumo = await resposta.Content.ReadFromJsonAsync<ResumoObrigacoesDto>(FabricaDaApi.Json);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(0, resumo!.Total);
        Assert.Null(resumo.MesInicio);
    }
}

public class SaudeEndpointTests : IClassFixture<FabricaDaApi>
{
    private readonly FabricaDaApi _api;

    public SaudeEndpointTests(FabricaDaApi api)
    {
        _api = api;
    }

    [Fact]
    public async Task Health_responde_sem_token_e_nao_depende_de_banco()
    {
        // Arrange
        var cliente = _api.CreateClient();

        // Act
        var resposta = await cliente.GetAsync("/health");
        var corpo = await resposta.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        using var json = JsonDocument.Parse(corpo);
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());

        var verificacoes = json.RootElement.GetProperty("verificacoes").EnumerateArray().ToList();
        var nomes = verificacoes.Select(v => v.GetProperty("nome").GetString()).ToList();

        Assert.Contains("self", nomes);
        Assert.DoesNotContain("oracle", nomes);
        Assert.DoesNotContain("mongo", nomes);
    }
}
