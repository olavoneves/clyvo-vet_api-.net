using Clyvo.Insights.Application.Excecoes;
using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Domain.Metas;
using Moq;

namespace Clyvo.Insights.Tests.Unit.Aplicacao.Metas;

public class RemoverMetaIndicadorTests : TesteDeCasoDeUsoDeMeta
{
    private RemoverMetaIndicador CasoDeUso() => new(Repositorio.Object, Tenant.Object);

    [Fact]
    public async Task ExecutarAsync_ComMetaExistente_RemoveEPersiste()
    {
        // Arrange
        var meta = Meta(IdClinicaDoToken, IndicadorMonitorado.TaxaCumprimento, 55m);
        Repositorio
            .Setup(r => r.ObterDaClinicaAsync(IdClinicaDoToken, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meta);

        // Act
        await CasoDeUso().ExecutarAsync(idMeta: 7);

        // Assert
        Repositorio.Verify(r => r.RemoverAsync(meta, It.IsAny<CancellationToken>()), Times.Once);
        Repositorio.Verify(r => r.SalvarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComMetaInexistente_LancaRecursoNaoEncontrado()
    {
        // Arrange
        Repositorio
            .Setup(r => r.ObterDaClinicaAsync(IdClinicaDoToken, 404, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MetaIndicador?)null);

        // Act
        var erro = await Record.ExceptionAsync(() => CasoDeUso().ExecutarAsync(idMeta: 404));

        // Assert
        Assert.IsType<RecursoNaoEncontradoException>(erro);
        Repositorio.Verify(
            r => r.RemoverAsync(It.IsAny<MetaIndicador>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
