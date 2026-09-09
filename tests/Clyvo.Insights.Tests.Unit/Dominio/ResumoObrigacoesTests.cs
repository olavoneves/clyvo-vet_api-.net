using Clyvo.Insights.Domain.Excecoes;
using Clyvo.Insights.Domain.Obrigacoes;

namespace Clyvo.Insights.Tests.Unit.Dominio;

public class ResumoObrigacoesTests
{
    [Fact]
    public void APartirDoFunil_ComFunilCumulativo_RetornaBaldesExclusivosQueSomamOTotal()
    {
        // Arrange — 100 vencidas: 80 notificadas, 60 responderam, 45 marcaram,
        // 30 vieram; 15 perdidas; 5 venceram sem notificação ou canceladas.
        const int total = 100;

        // Act
        var resumo = ResumoObrigacoes.APartirDoFunil(
            total,
            alcancaramNotificada: 80,
            alcancaramRespondida: 60,
            alcancaramAgendada: 45,
            cumpridas: 30,
            perdidas: 15);

        // Assert
        Assert.Equal(30, resumo.Cumpridas);
        Assert.Equal(15, resumo.PararamEmAgendada);
        Assert.Equal(15, resumo.PararamEmRespondida);
        Assert.Equal(20, resumo.PararamEmNotificada);
        Assert.Equal(15, resumo.Perdidas);
        Assert.Equal(5, resumo.NaoTrabalhadas);

        var soma = resumo.Cumpridas + resumo.PararamEmAgendada + resumo.PararamEmRespondida
                   + resumo.PararamEmNotificada + resumo.Perdidas + resumo.NaoTrabalhadas;
        Assert.Equal(total, soma);
    }

    [Fact]
    public void APartirDoFunil_SemEstadoIntermediario_ConcentraEmCumpridasEPerdidas()
    {
        // Arrange — a forma do banco de demonstração: todo mundo que venceu já
        // terminou, e nenhuma obrigação parou no meio do caminho.
        const int total = 2715;

        // Act
        var resumo = ResumoObrigacoes.APartirDoFunil(
            total,
            alcancaramNotificada: 1601,
            alcancaramRespondida: 1601,
            alcancaramAgendada: 1601,
            cumpridas: 1601,
            perdidas: 1114);

        // Assert
        Assert.Equal(1601, resumo.Cumpridas);
        Assert.Equal(1114, resumo.Perdidas);
        Assert.Equal(0, resumo.PararamEmAgendada);
        Assert.Equal(0, resumo.PararamEmRespondida);
        Assert.Equal(0, resumo.PararamEmNotificada);
        Assert.Equal(0, resumo.NaoTrabalhadas);
    }

    [Fact]
    public void APartirDoFunil_SemObrigacaoVencida_RetornaResumoZerado()
    {
        // Arrange
        const int nenhuma = 0;

        // Act
        var resumo = ResumoObrigacoes.APartirDoFunil(nenhuma, 0, 0, 0, 0, 0);

        // Assert
        Assert.Equal(0, resumo.Total);
        Assert.Equal(0, resumo.NaoTrabalhadas);
    }

    [Theory]
    [InlineData(50, 60, 40, 30)]   // respondida acima de notificada
    [InlineData(80, 40, 50, 30)]   // agendada acima de respondida
    [InlineData(80, 60, 40, 50)]   // cumprida acima de agendada
    public void APartirDoFunil_ComDegrauForaDeOrdem_LancaRegraDeDominio(
        int notificada, int respondida, int agendada, int cumpridas)
    {
        // Arrange
        const int total = 100;

        // Act
        var erro = Record.Exception(
            () => ResumoObrigacoes.APartirDoFunil(total, notificada, respondida, agendada, cumpridas, perdidas: 0));

        // Assert
        var regra = Assert.IsType<RegraDeDominioException>(erro);
        Assert.Contains("degrau", regra.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void APartirDoFunil_ComNotificadasMaisPerdidasAcimaDoTotal_LancaRegraDeDominio()
    {
        // Arrange — os dois conjuntos são disjuntos e não cabem no total
        const int total = 100;

        // Act
        var erro = Record.Exception(
            () => ResumoObrigacoes.APartirDoFunil(total, 80, 60, 45, 30, perdidas: 40));

        // Assert
        Assert.IsType<RegraDeDominioException>(erro);
    }

    [Fact]
    public void APartirDoFunil_ComContagemNegativa_LancaRegraDeDominio()
    {
        // Arrange
        const int totalNegativo = -1;

        // Act
        var erro = Record.Exception(
            () => ResumoObrigacoes.APartirDoFunil(totalNegativo, 0, 0, 0, 0, 0));

        // Assert
        Assert.IsType<RegraDeDominioException>(erro);
    }
}
