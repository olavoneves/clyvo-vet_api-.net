using Clyvo.Insights.Application.Excecoes;
using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Domain.Metas;
using Moq;

namespace Clyvo.Insights.Tests.Unit.Aplicacao.Metas;

public class ObterMetaIndicadorTests : TesteDeCasoDeUsoDeMeta
{
    private ObterMetaIndicador CasoDeUso() => new(Repositorio.Object, Tenant.Object);

    [Fact]
    public async Task ExecutarAsync_ComMetaDaClinicaDoToken_DevolveODto()
    {
        // Arrange
        Repositorio
            .Setup(r => r.ObterDaClinicaAsync(IdClinicaDoToken, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Meta(IdClinicaDoToken, IndicadorMonitorado.TaxaCumprimento, 55m));

        // Act
        var meta = await CasoDeUso().ExecutarAsync(5);

        // Assert
        Assert.Equal("TaxaCumprimento", meta.Indicador);
        Assert.Equal(55m, meta.LimiarPercentual);
    }

    [Fact]
    public async Task ExecutarAsync_ComMetaDeOutraClinica_LancaRecursoNaoEncontrado()
    {
        // Arrange — com o recorte por clínica, a meta da vizinha não é achada
        Repositorio
            .Setup(r => r.ObterDaClinicaAsync(IdClinicaDoToken, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MetaIndicador?)null);

        // Act & Assert
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => CasoDeUso().ExecutarAsync(5));

        Repositorio.Verify(
            r => r.ObterDaClinicaAsync(IdClinicaVizinha, It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
