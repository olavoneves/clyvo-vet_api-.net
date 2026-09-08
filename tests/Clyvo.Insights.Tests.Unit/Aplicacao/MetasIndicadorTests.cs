using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Excecoes;
using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Domain.Metas;
using Moq;

namespace Clyvo.Insights.Tests.Unit.Aplicacao;

/// <summary>
/// CRUD de meta com repositório mockado: o que se verifica é o recorte por
/// tenant e a ordem das chamadas, não a persistência.
/// </summary>
public class MetasIndicadorTests
{
    private const long IdClinicaDoToken = 23;
    private const long IdClinicaVizinha = 24;

    private readonly Mock<IMetaIndicadorRepository> _repositorio = new();
    private readonly Mock<ITenantContext> _tenant = new();

    public MetasIndicadorTests()
    {
        _tenant.SetupGet(t => t.IdClinica).Returns(IdClinicaDoToken);
    }

    private static MetaIndicador Meta(long idClinica, IndicadorMonitorado indicador, decimal limiar) =>
        MetaIndicador.Criar(idClinica, indicador, limiar, DateTime.UtcNow);

    [Fact]
    public async Task Criar_grava_a_meta_na_clinica_do_token()
    {
        // Arrange
        MetaIndicador? gravada = null;
        _repositorio
            .Setup(r => r.ExisteParaIndicadorAsync(IdClinicaDoToken, IndicadorMonitorado.TaxaCumprimento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositorio
            .Setup(r => r.AdicionarAsync(It.IsAny<MetaIndicador>(), It.IsAny<CancellationToken>()))
            .Callback<MetaIndicador, CancellationToken>((m, _) => gravada = m)
            .Returns(Task.CompletedTask);

        var casoDeUso = new CriarMetaIndicador(_repositorio.Object, _tenant.Object);

        // Act
        var dto = await casoDeUso.ExecutarAsync(IndicadorMonitorado.TaxaCumprimento, 55m);

        // Assert
        Assert.NotNull(gravada);
        Assert.Equal(IdClinicaDoToken, gravada!.IdClinica);
        Assert.Equal(nameof(IndicadorMonitorado.TaxaCumprimento), dto.Indicador);
        Assert.Equal(55m, dto.LimiarPercentual);
        _repositorio.Verify(r => r.SalvarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Criar_segunda_meta_para_o_mesmo_indicador_e_conflito_e_nao_grava()
    {
        // Arrange
        _repositorio
            .Setup(r => r.ExisteParaIndicadorAsync(IdClinicaDoToken, IndicadorMonitorado.TaxaCumprimento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var casoDeUso = new CriarMetaIndicador(_repositorio.Object, _tenant.Object);

        // Act
        var erro = await Record.ExceptionAsync(
            () => casoDeUso.ExecutarAsync(IndicadorMonitorado.TaxaCumprimento, 55m));

        // Assert
        Assert.IsType<ConflitoDeRecursoException>(erro);
        _repositorio.Verify(r => r.AdicionarAsync(It.IsAny<MetaIndicador>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositorio.Verify(r => r.SalvarAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Listar_pede_ao_repositorio_so_a_clinica_do_token()
    {
        // Arrange
        _repositorio
            .Setup(r => r.ListarDaClinicaAsync(IdClinicaDoToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                Meta(IdClinicaDoToken, IndicadorMonitorado.TaxaCumprimento, 55m),
                Meta(IdClinicaDoToken, IndicadorMonitorado.DeltaPontosPercentuais, 20m)
            });

        var casoDeUso = new ListarMetasIndicador(_repositorio.Object, _tenant.Object);

        // Act
        var metas = await casoDeUso.ExecutarAsync();

        // Assert
        Assert.Equal(2, metas.Count);
        _repositorio.Verify(r => r.ListarDaClinicaAsync(IdClinicaDoToken, It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.Verify(r => r.ListarDaClinicaAsync(IdClinicaVizinha, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Atualizar_move_o_piso_e_persiste()
    {
        // Arrange
        var meta = Meta(IdClinicaDoToken, IndicadorMonitorado.TaxaCumprimento, 55m);
        _repositorio
            .Setup(r => r.ObterDaClinicaAsync(IdClinicaDoToken, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meta);

        var casoDeUso = new AtualizarMetaIndicador(_repositorio.Object, _tenant.Object);

        // Act
        var dto = await casoDeUso.ExecutarAsync(idMeta: 7, limiarPercentual: 62.5m);

        // Assert
        Assert.Equal(62.5m, dto.LimiarPercentual);
        Assert.Equal(62.5m, meta.LimiarPercentual);
        _repositorio.Verify(r => r.SalvarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Atualizar_meta_de_outra_clinica_nao_encontra()
    {
        // Arrange — o repositório filtra por tenant, então a meta da vizinha não volta
        _repositorio
            .Setup(r => r.ObterDaClinicaAsync(IdClinicaDoToken, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MetaIndicador?)null);

        var casoDeUso = new AtualizarMetaIndicador(_repositorio.Object, _tenant.Object);

        // Act
        var erro = await Record.ExceptionAsync(() => casoDeUso.ExecutarAsync(idMeta: 7, limiarPercentual: 62.5m));

        // Assert
        Assert.IsType<RecursoNaoEncontradoException>(erro);
        _repositorio.Verify(r => r.SalvarAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Remover_apaga_e_persiste()
    {
        // Arrange
        var meta = Meta(IdClinicaDoToken, IndicadorMonitorado.TaxaCumprimento, 55m);
        _repositorio
            .Setup(r => r.ObterDaClinicaAsync(IdClinicaDoToken, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meta);

        var casoDeUso = new RemoverMetaIndicador(_repositorio.Object, _tenant.Object);

        // Act
        await casoDeUso.ExecutarAsync(idMeta: 7);

        // Assert
        _repositorio.Verify(r => r.RemoverAsync(meta, It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.Verify(r => r.SalvarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Remover_meta_inexistente_nao_encontra()
    {
        // Arrange
        _repositorio
            .Setup(r => r.ObterDaClinicaAsync(IdClinicaDoToken, 404, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MetaIndicador?)null);

        var casoDeUso = new RemoverMetaIndicador(_repositorio.Object, _tenant.Object);

        // Act
        var erro = await Record.ExceptionAsync(() => casoDeUso.ExecutarAsync(idMeta: 404));

        // Assert
        Assert.IsType<RecursoNaoEncontradoException>(erro);
        _repositorio.Verify(r => r.RemoverAsync(It.IsAny<MetaIndicador>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
