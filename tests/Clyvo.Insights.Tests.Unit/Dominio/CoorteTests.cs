using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Domain.Excecoes;

namespace Clyvo.Insights.Tests.Unit.Dominio;

public class CoorteTests
{
    [Fact]
    public void TaxaCumprimento_e_derivada_das_contagens()
    {
        // Arrange
        const int resolvidas = 2715;
        const int cumpridas = 1601;

        // Act
        var coorte = Coorte.Criar(GrupoCoorte.Tratado, resolvidas, cumpridas, petsDistintos: 218);

        // Assert
        Assert.Equal((decimal)cumpridas / resolvidas, coorte.TaxaCumprimento);
        Assert.False(coorte.Vazia);
    }

    [Fact]
    public void Coorte_sem_obrigacao_resolvida_e_vazia_e_nao_divide_por_zero()
    {
        // Arrange
        const int semDenominador = 0;

        // Act
        var coorte = Coorte.Criar(GrupoCoorte.Controle, semDenominador, obrigacoesCumpridas: 0, petsDistintos: 0);

        // Assert
        Assert.True(coorte.Vazia);
        Assert.Equal(0m, coorte.TaxaCumprimento);
    }

    [Fact]
    public void Taxa_de_cem_por_cento_vale_um_inteiro()
    {
        // Arrange
        const int todas = 40;

        // Act
        var coorte = Coorte.Criar(GrupoCoorte.Tratado, todas, todas, petsDistintos: 12);

        // Assert
        Assert.Equal(1m, coorte.TaxaCumprimento);
    }

    [Fact]
    public void Cumpridas_acima_de_resolvidas_e_erro_de_quem_chamou()
    {
        // Arrange
        const int resolvidas = 10;
        const int cumpridas = 11;

        // Act
        var erro = Record.Exception(
            () => Coorte.Criar(GrupoCoorte.Tratado, resolvidas, cumpridas, petsDistintos: 5));

        // Assert
        var regra = Assert.IsType<RegraDeDominioException>(erro);
        Assert.Contains("obrigações", regra.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(10, -1, 0)]
    [InlineData(10, 5, -1)]
    public void Contagem_negativa_e_erro_de_quem_chamou(int resolvidas, int cumpridas, int pets)
    {
        // Arrange
        const GrupoCoorte grupo = GrupoCoorte.Tratado;

        // Act
        var erro = Record.Exception(() => Coorte.Criar(grupo, resolvidas, cumpridas, pets));

        // Assert
        Assert.IsType<RegraDeDominioException>(erro);
    }
}
