using Clyvo.Insights.Domain.Excecoes;
using Clyvo.Insights.Domain.Metas;

namespace Clyvo.Insights.Tests.Unit.Dominio;

public class MetaIndicadorTests
{
    private static readonly DateTime Agora = new(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Criar_ComDadosValidos_NasceComAsDuasDatasIguais()
    {
        // Arrange
        const decimal limiar = 55m;

        // Act
        var meta = MetaIndicador.Criar(23, IndicadorMonitorado.TaxaCumprimento, limiar, Agora);

        // Assert
        Assert.Equal(23, meta.IdClinica);
        Assert.Equal(IndicadorMonitorado.TaxaCumprimento, meta.Indicador);
        Assert.Equal(limiar, meta.LimiarPercentual);
        Assert.Equal(Agora, meta.DataCriacao);
        Assert.Equal(Agora, meta.DataAtualizacao);
    }

    [Fact]
    public void AlterarLimiar_ComLimiarValido_MoveOPisoECarimbaAAtualizacao()
    {
        // Arrange
        var meta = MetaIndicador.Criar(23, IndicadorMonitorado.TaxaCumprimento, 55m, Agora);
        var depois = Agora.AddDays(1);

        // Act
        meta.AlterarLimiar(62.5m, depois);

        // Assert
        Assert.Equal(62.5m, meta.LimiarPercentual);
        Assert.Equal(Agora, meta.DataCriacao);
        Assert.Equal(depois, meta.DataAtualizacao);
    }

    [Fact]
    public void EstaAbaixoDoLimiar_ComValorAbaixoDoPiso_RetornaVerdadeiro()
    {
        // Arrange
        var meta = MetaIndicador.Criar(23, IndicadorMonitorado.TaxaCumprimento, 55m, Agora);

        // Act
        var abaixo = meta.EstaAbaixoDoLimiar(54.99m);

        // Assert
        Assert.True(abaixo);
    }

    [Fact]
    public void EstaAbaixoDoLimiar_ComValorExatamenteNoPiso_RetornaFalso()
    {
        // Arrange
        var meta = MetaIndicador.Criar(23, IndicadorMonitorado.TaxaCumprimento, 55m, Agora);

        // Act
        var abaixo = meta.EstaAbaixoDoLimiar(55m);

        // Assert
        Assert.False(abaixo);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Criar_ComLimiarForaDaFaixa_LancaRegraDeDominio(decimal limiar)
    {
        // Arrange
        const long idClinica = 23;

        // Act
        var erro = Record.Exception(
            () => MetaIndicador.Criar(idClinica, IndicadorMonitorado.TaxaCumprimento, limiar, Agora));

        // Assert
        Assert.IsType<RegraDeDominioException>(erro);
    }

    [Fact]
    public void AlterarLimiar_ComLimiarForaDaFaixa_LancaSemAlterarAMeta()
    {
        // Arrange
        var meta = MetaIndicador.Criar(23, IndicadorMonitorado.TaxaCumprimento, 55m, Agora);

        // Act
        var erro = Record.Exception(() => meta.AlterarLimiar(101m, Agora.AddDays(1)));

        // Assert
        Assert.IsType<RegraDeDominioException>(erro);
        Assert.Equal(55m, meta.LimiarPercentual);
        Assert.Equal(Agora, meta.DataAtualizacao);
    }

    [Fact]
    public void Criar_ComIndicadorForaDoEnum_LancaRegraDeDominio()
    {
        // Arrange
        const IndicadorMonitorado inexistente = (IndicadorMonitorado)99;

        // Act
        var erro = Record.Exception(() => MetaIndicador.Criar(23, inexistente, 55m, Agora));

        // Assert
        Assert.IsType<RegraDeDominioException>(erro);
    }

    [Fact]
    public void Criar_ComClinicaInvalida_LancaRegraDeDominio()
    {
        // Arrange
        const long semClinica = 0;

        // Act
        var erro = Record.Exception(
            () => MetaIndicador.Criar(semClinica, IndicadorMonitorado.TaxaCumprimento, 55m, Agora));

        // Assert
        Assert.IsType<RegraDeDominioException>(erro);
    }
}
