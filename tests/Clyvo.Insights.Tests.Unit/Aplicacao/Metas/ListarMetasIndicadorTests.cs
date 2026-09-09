using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Domain.Metas;
using Moq;

namespace Clyvo.Insights.Tests.Unit.Aplicacao.Metas;

public class ListarMetasIndicadorTests : TesteDeCasoDeUsoDeMeta
{
    private ListarMetasIndicador CasoDeUso() => new(Repositorio.Object, Tenant.Object);

    [Fact]
    public async Task ExecutarAsync_ComMetasDeVariasClinicas_ListaSomenteAsDaClinicaDoToken()
    {
        // Arrange
        Repositorio
            .Setup(r => r.ListarDaClinicaAsync(IdClinicaDoToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                Meta(IdClinicaDoToken, IndicadorMonitorado.TaxaCumprimento, 55m),
                Meta(IdClinicaDoToken, IndicadorMonitorado.DeltaPontosPercentuais, 20m)
            });

        // Act
        var metas = await CasoDeUso().ExecutarAsync();

        // Assert
        Assert.Equal(2, metas.Count);
        Repositorio.Verify(
            r => r.ListarDaClinicaAsync(IdClinicaDoToken, It.IsAny<CancellationToken>()), Times.Once);
        Repositorio.Verify(
            r => r.ListarDaClinicaAsync(IdClinicaVizinha, It.IsAny<CancellationToken>()), Times.Never);
    }
}
