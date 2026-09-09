using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Clyvo.Insights.Application.Obrigacoes;
using Clyvo.Insights.Tests.Integration.Fixtures;

namespace Clyvo.Insights.Tests.Integration;

[Collection(ColecaoDaApi.Nome)]
public class ObrigacoesEndpointTests
{
    private const string Rota = "/api/insights/obrigacoes/resumo";

    private readonly FabricaDaApi _api;

    public ObrigacoesEndpointTests(FabricaDaApi api)
    {
        _api = api;
    }

    [Fact]
    public async Task ObterResumoObrigacoes_SemToken_Retorna401()
    {
        // Arrange
        var cliente = _api.CreateClient();

        // Act
        var resposta = await cliente.GetAsync(Rota);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task ObterResumoObrigacoes_ComObrigacoesVencidas_RetornaBaldesQueSomamOTotal()
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
    public async Task ObterResumoObrigacoes_SemObrigacaoVencida_Retorna200Zerado()
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

[Collection(ColecaoDaApi.Nome)]
public class SaudeEndpointTests
{
    private readonly FabricaDaApi _api;

    public SaudeEndpointTests(FabricaDaApi api)
    {
        _api = api;
    }

    [Fact]
    public async Task Health_SemToken_Retorna200ComApenasOCheckSelf()
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

    [Fact]
    public async Task Metrics_SemToken_Retorna200ComDuracaoPorRotaETaxaDeErro()
    {
        // Arrange — uma requisição autenticada e uma sem token, para que exista
        // pelo menos uma resposta em cada faixa
        var autenticado = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        await autenticado.GetAsync("/api/insights/coorte");
        await _api.CreateClient().GetAsync("/api/insights/coorte");

        // Act
        var resposta = await _api.CreateClient().GetAsync("/metrics");
        var corpo = await resposta.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        using var json = JsonDocument.Parse(corpo);
        Assert.True(json.RootElement.GetProperty("requisicoes").GetInt64() > 0);
        Assert.True(json.RootElement.TryGetProperty("taxaDeErro", out _));

        var rotas = json.RootElement.GetProperty("duracaoPorRota").EnumerateArray()
            .Select(r => r.GetProperty("rota").GetString())
            .ToList();

        // Rota, e não caminho: o que aparece é o template registrado.
        Assert.Contains("GET api/insights/coorte", rotas);
    }
}
