using Clyvo.Insights.Domain.Obrigacoes;

namespace Clyvo.Insights.Tests.Unit.Dominio;

public class StatusObrigacaoTests
{
    [Theory]
    [InlineData(StatusObrigacao.Prevista, "PREVISTA")]
    [InlineData(StatusObrigacao.Cumprida, "CUMPRIDA")]
    [InlineData(StatusObrigacao.Cancelada, "CANCELADA")]
    public void Codigo_DeCadaStatus_DevolveONomeEmMaiusculasComoOCoreGrava(StatusObrigacao status, string esperado)
    {
        // Act
        var codigo = status.Codigo();

        // Assert
        Assert.Equal(esperado, codigo);
    }

    [Theory]
    [InlineData("CUMPRIDA", StatusObrigacao.Cumprida)]
    [InlineData("cumprida", StatusObrigacao.Cumprida)]
    [InlineData("  Agendada ", StatusObrigacao.Agendada)]
    public void TentarInterpretar_ComCodigoConhecido_IgnoraCaixaEEspacos(string codigo, StatusObrigacao esperado)
    {
        // Act
        var interpretou = CodigoDeStatus.TentarInterpretar(codigo, out var status);

        // Assert
        Assert.True(interpretou);
        Assert.Equal(esperado, status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ATRASADA")]
    [InlineData("3")]
    public void TentarInterpretar_ComCodigoDesconhecidoOuNumero_Recusa(string? codigo)
    {
        // Act
        var interpretou = CodigoDeStatus.TentarInterpretar(codigo, out _);

        // Assert — "3" recusado: Enum.TryParse o aceitaria como Respondida
        Assert.False(interpretou);
    }

    [Fact]
    public void StatusObrigacao_OrdemNumerica_SegueOFunilDoMotor()
    {
        // Arrange
        var esperado = new[] { "PREVISTA", "NOTIFICADA", "RESPONDIDA", "AGENDADA", "CUMPRIDA", "PERDIDA", "CANCELADA" };

        // Act
        var ordem = Enum.GetValues<StatusObrigacao>().OrderBy(s => (int)s).Select(s => s.Codigo());

        // Assert — a ordenação por status da listagem depende desta ordem
        Assert.Equal(esperado, ordem);
    }
}
