using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Tests.Integration.Fixtures;

namespace Clyvo.Insights.Tests.Integration;

[Collection(ColecaoDaApi.Nome)]
public class CoorteEndpointTests
{
    private const string Rota = "/api/insights/coorte";

    private readonly FabricaDaApi _api;

    public CoorteEndpointTests(FabricaDaApi api)
    {
        _api = api;
    }

    [Fact]
    public async Task ObterCoorte_SemToken_Retorna401()
    {
        // Arrange
        var cliente = _api.CreateClient();

        // Act
        var resposta = await cliente.GetAsync(Rota);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task ObterCoorte_ComTokenDeOutraChave_Retorna401()
    {
        // Arrange
        var cliente = _api.CreateClient();
        var tokenIntruso = FabricaDaApi.TokenDaClinica(
            DadosCanonicos.ClinicaComCoorte,
            segredo: "outra-chave-completamente-diferente-com-32-bytes");

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenIntruso);

        // Act
        var resposta = await cliente.GetAsync(Rota);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task ObterCoorte_ComTokenSemClaimDeClinica_Retorna403()
    {
        // Arrange
        var cliente = _api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", FabricaDaApi.TokenSemClinica());

        // Act
        var resposta = await cliente.GetAsync(Rota);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task ObterCoorte_ComTokenValido_Retorna200ComAAnaliseDaClinica()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var resposta = await cliente.GetAsync(Rota);
        var analise = await resposta.Content.ReadFromJsonAsync<AnaliseCoorteDto>(FabricaDaApi.Json);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.NotNull(analise);
        Assert.Equal(DadosCanonicos.ClinicaComCoorte, analise!.IdClinica);
        Assert.True(analise.Disponivel);
        Assert.Null(analise.MotivoIndisponibilidade);

        Assert.Equal("TRATADO", analise.Tratado.Grupo);
        Assert.Equal("CONTROLE", analise.Controle.Grupo);
        Assert.Equal(60.00m, analise.Tratado.TaxaCumprimentoPercentual);
        Assert.Equal(40.00m, analise.Controle.TaxaCumprimentoPercentual);
    }

    [Fact]
    public async Task ObterCoorte_ComAnaliseDisponivel_RetornaAritmeticaConsistenteComAsTaxas()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        var analise = await cliente.GetFromJsonAsync<AnaliseCoorteDto>(Rota, FabricaDaApi.Json);

        // Assert — cada número é conferido contra os outros da mesma resposta,
        // e não contra uma constante do seed.
        Assert.NotNull(analise);
        var delta = analise!.Tratado.TaxaCumprimentoPercentual - analise.Controle.TaxaCumprimentoPercentual;

        Assert.Equal(delta, analise.DeltaPontosPercentuais);
        Assert.Equal(
            Math.Round(analise.Tratado.ObrigacoesResolvidas * delta / 100m, 2),
            analise.ConsultasAtribuiveis);
        Assert.Equal(
            Math.Round(analise.ConsultasAtribuiveis * analise.TicketMedio!.Value, 2),
            analise.ReceitaRecuperada);
        Assert.True(analise.ReceitaEstimavel);
    }

    [Fact]
    public async Task ObterCoorte_ComTokenDeOutraClinica_RetornaDadosDaquelaClinica()
    {
        // Arrange
        var daClinica = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);
        var daVizinha = _api.ClienteDaClinica(DadosCanonicos.ClinicaVizinha);

        // Act
        var analiseDaClinica = await daClinica.GetFromJsonAsync<AnaliseCoorteDto>(Rota, FabricaDaApi.Json);
        var analiseDaVizinha = await daVizinha.GetFromJsonAsync<AnaliseCoorteDto>(Rota, FabricaDaApi.Json);

        // Assert
        Assert.Equal(DadosCanonicos.ClinicaComCoorte, analiseDaClinica!.IdClinica);
        Assert.Equal(DadosCanonicos.ClinicaVizinha, analiseDaVizinha!.IdClinica);

        Assert.Equal(60.00m, analiseDaClinica.Tratado.TaxaCumprimentoPercentual);
        Assert.Equal(50.00m, analiseDaVizinha.Tratado.TaxaCumprimentoPercentual);
        Assert.NotEqual(analiseDaClinica.DeltaPontosPercentuais, analiseDaVizinha.DeltaPontosPercentuais);
        Assert.NotEqual(analiseDaClinica.TicketMedio, analiseDaVizinha.TicketMedio);
    }

    [Fact]
    public async Task ObterCoorte_ComClinicaSemCoorte_Retorna200ComAnaliseIndisponivel()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaSemDados);

        // Act
        var resposta = await cliente.GetAsync(Rota);
        var analise = await resposta.Content.ReadFromJsonAsync<AnaliseCoorteDto>(FabricaDaApi.Json);

        // Assert — ausência de contrafactual não é erro de requisição
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.False(analise!.Disponivel);
        Assert.NotNull(analise.MotivoIndisponibilidade);
        Assert.Equal(0m, analise.DeltaPontosPercentuais);
        Assert.Empty(analise.Metas);
    }

    [Fact]
    public async Task ObterCoorte_ComTokenValido_RegistraConsultaESnapshotNaProjecao()
    {
        // Arrange
        var cliente = _api.ClienteDaClinica(DadosCanonicos.ClinicaComCoorte);

        // Act
        await cliente.GetAsync(Rota);

        // Assert
        Assert.Contains(_api.Projecoes.Consultas, c => c.IdClinica == DadosCanonicos.ClinicaComCoorte);
        Assert.Contains(_api.Projecoes.Snapshots, s => s.IdClinica == DadosCanonicos.ClinicaComCoorte);
    }
}
