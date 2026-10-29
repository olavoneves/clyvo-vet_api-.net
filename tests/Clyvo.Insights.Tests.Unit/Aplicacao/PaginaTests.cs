using Clyvo.Insights.Application.Paginacao;

namespace Clyvo.Insights.Tests.Unit.Aplicacao;

public class PaginaTests
{
    private static Pagina<int> Pagina(int numero, int tamanho, int total) =>
        new(Enumerable.Range(1, Math.Max(0, Math.Min(tamanho, total - (numero - 1) * tamanho))).ToList(),
            numero, tamanho, total);

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(7840, 20, 392)]
    public void TotalPaginas_ComTotalETamanho_ArredondaParaCima(int total, int tamanho, int esperado)
    {
        // Act
        var pagina = Pagina(1, tamanho, total);

        // Assert
        Assert.Equal(esperado, pagina.TotalPaginas);
    }

    [Fact]
    public void TotalPaginas_ComTotalPertoDoLimiteDoInt_NaoEstoura()
    {
        // Arrange — total + tamanho passaria de int.MaxValue numa conta em int
        var pagina = new Pagina<int>(Array.Empty<int>(), 1, 100, int.MaxValue);

        // Act
        var total = pagina.TotalPaginas;

        // Assert
        Assert.Equal(21_474_837, total);
    }

    [Fact]
    public void Navegacao_NaPrimeiraPagina_TemProximaENaoTemAnterior()
    {
        // Arrange
        var pagina = Pagina(1, 20, 45);

        // Act & Assert
        Assert.False(pagina.TemAnterior);
        Assert.True(pagina.TemProxima);
        Assert.Equal(2, pagina.Proxima);
        Assert.Equal(3, pagina.Ultima);
    }

    [Fact]
    public void Navegacao_NaPaginaDoMeio_TemAnteriorEProxima()
    {
        // Arrange
        var pagina = Pagina(2, 20, 45);

        // Act & Assert
        Assert.True(pagina.TemAnterior);
        Assert.Equal(1, pagina.Anterior);
        Assert.True(pagina.TemProxima);
        Assert.Equal(3, pagina.Proxima);
    }

    [Fact]
    public void Navegacao_NaUltimaPagina_TemAnteriorENaoTemProxima()
    {
        // Arrange
        var pagina = Pagina(3, 20, 45);

        // Act & Assert
        Assert.True(pagina.TemAnterior);
        Assert.Equal(2, pagina.Anterior);
        Assert.False(pagina.TemProxima);
        Assert.Equal(5, pagina.Itens.Count);
    }

    [Fact]
    public void Navegacao_AlemDaUltimaPagina_AnteriorApontaParaAUltimaENaoParaAVizinhaVazia()
    {
        // Arrange
        var pagina = new Pagina<int>(Array.Empty<int>(), 50, 20, 45);

        // Act & Assert
        Assert.True(pagina.TemAnterior);
        Assert.Equal(3, pagina.Anterior);
        Assert.False(pagina.TemProxima);
    }

    [Fact]
    public void Navegacao_SemItens_UltimaEAPrimeiraESemVizinhas()
    {
        // Arrange
        var pagina = new Pagina<int>(Array.Empty<int>(), 1, 20, 0);

        // Act & Assert
        Assert.Equal(0, pagina.TotalPaginas);
        Assert.Equal(1, pagina.Ultima);
        Assert.False(pagina.TemAnterior);
        Assert.False(pagina.TemProxima);
    }

    [Fact]
    public void Mapear_ComProjecao_PreservaNumeroTamanhoETotal()
    {
        // Arrange
        var pagina = new Pagina<int>(new[] { 1, 2 }, 2, 2, 5);

        // Act
        var mapeada = pagina.Mapear(i => $"item {i}");

        // Assert
        Assert.Equal(new[] { "item 1", "item 2" }, mapeada.Itens);
        Assert.Equal(2, mapeada.Numero);
        Assert.Equal(2, mapeada.Tamanho);
        Assert.Equal(5, mapeada.TotalItens);
    }
}
