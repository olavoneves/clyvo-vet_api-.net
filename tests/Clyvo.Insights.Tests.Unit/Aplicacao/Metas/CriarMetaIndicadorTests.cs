using Clyvo.Insights.Application.Excecoes;
using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Domain.Metas;
using Moq;

namespace Clyvo.Insights.Tests.Unit.Aplicacao.Metas;

public class CriarMetaIndicadorTests : TesteDeCasoDeUsoDeMeta
{
    private CriarMetaIndicador CasoDeUso() => new(Repositorio.Object, Tenant.Object);

    [Fact]
    public async Task ExecutarAsync_ComIndicadorInedito_GravaMetaNaClinicaDoToken()
    {
        // Arrange
        MetaIndicador? gravada = null;
        Repositorio
            .Setup(r => r.ExisteParaIndicadorAsync(
                IdClinicaDoToken, IndicadorMonitorado.TaxaCumprimento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        Repositorio
            .Setup(r => r.AdicionarAsync(It.IsAny<MetaIndicador>(), It.IsAny<CancellationToken>()))
            .Callback<MetaIndicador, CancellationToken>((m, _) => gravada = m)
            .Returns(Task.CompletedTask);

        // Act
        var dto = await CasoDeUso().ExecutarAsync(IndicadorMonitorado.TaxaCumprimento, 55m);

        // Assert
        Assert.NotNull(gravada);
        Assert.Equal(IdClinicaDoToken, gravada!.IdClinica);
        Assert.Equal(nameof(IndicadorMonitorado.TaxaCumprimento), dto.Indicador);
        Assert.Equal(55m, dto.LimiarPercentual);
        Repositorio.Verify(r => r.SalvarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComIndicadorJaComMeta_LancaConflitoSemGravar()
    {
        // Arrange
        Repositorio
            .Setup(r => r.ExisteParaIndicadorAsync(
                IdClinicaDoToken, IndicadorMonitorado.TaxaCumprimento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var erro = await Record.ExceptionAsync(
            () => CasoDeUso().ExecutarAsync(IndicadorMonitorado.TaxaCumprimento, 55m));

        // Assert
        Assert.IsType<ConflitoDeRecursoException>(erro);
        Repositorio.Verify(
            r => r.AdicionarAsync(It.IsAny<MetaIndicador>(), It.IsAny<CancellationToken>()), Times.Never);
        Repositorio.Verify(r => r.SalvarAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
