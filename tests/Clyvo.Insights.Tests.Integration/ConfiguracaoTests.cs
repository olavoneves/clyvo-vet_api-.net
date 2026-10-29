using Clyvo.Insights.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clyvo.Insights.Tests.Integration;

/// <summary>
/// A connection string do Oracle não tem valor padrão: sem ela, o registro da
/// infraestrutura falha na hora, e diz o que definir.
/// </summary>
public class ConfiguracaoTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddInfrastructure_SemConnectionStringDoOracle_FalhaDizendoQualVariavelDefinir(string? valor)
    {
        // Arrange
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Oracle"] = valor })
            .Build();

        // Act
        var excecao = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddInfrastructure(configuracao));

        // Assert
        Assert.Contains("ConnectionStrings__Oracle", excecao.Message);
        Assert.DoesNotContain("fiap", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }
}
