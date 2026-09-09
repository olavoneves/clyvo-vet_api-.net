using Clyvo.Insights.Application.Excecoes;
using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Domain.Metas;
using Moq;

namespace Clyvo.Insights.Tests.Unit.Aplicacao.Metas;

public class AtualizarMetaIndicadorTests : TesteDeCasoDeUsoDeMeta
{
    private AtualizarMetaIndicador CasoDeUso() => new(Repositorio.Object, Tenant.Object);

    [Fact]
    public async Task ExecutarAsync_ComMetaExistente_MoveOPisoEPersiste()
    {
        // Arrange
        var meta = Meta(IdClinicaDoToken, IndicadorMonitorado.TaxaCumprimento, 55m);
        Repositorio
            .Setup(r => r.ObterDaClinicaAsync(IdClinicaDoToken, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meta);

        // Act
        var dto = await CasoDeUso().ExecutarAsync(idMeta: 7, limiarPercentual: 62.5m);

        // Assert
        Assert.Equal(62.5m, dto.LimiarPercentual);
        Assert.Equal(62.5m, meta.LimiarPercentual);
        Repositorio.Verify(r => r.SalvarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComMetaDeOutraClinica_LancaRecursoNaoEncontrado()
    {
        // Arrange — o repositório filtra por tenant, então a meta da vizinha não volta
        Repositorio
            .Setup(r => r.ObterDaClinicaAsync(IdClinicaDoToken, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MetaIndicador?)null);

        // Act
        var erro = await Record.ExceptionAsync(
            () => CasoDeUso().ExecutarAsync(idMeta: 7, limiarPercentual: 62.5m));

        // Assert
        Assert.IsType<RecursoNaoEncontradoException>(erro);
        Repositorio.Verify(r => r.SalvarAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
