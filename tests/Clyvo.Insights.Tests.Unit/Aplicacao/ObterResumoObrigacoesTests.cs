using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Obrigacoes;
using Moq;

namespace Clyvo.Insights.Tests.Unit.Aplicacao;

public class ObterResumoObrigacoesTests
{
    private const long IdClinicaDoToken = 23;

    private readonly Mock<ICoorteReadRepository> _repositorio = new(MockBehavior.Strict);
    private readonly Mock<ITenantContext> _tenant = new();

    public ObterResumoObrigacoesTests()
    {
        _tenant.SetupGet(t => t.IdClinica).Returns(IdClinicaDoToken);
    }

    [Fact]
    public async Task Consulta_o_funil_da_clinica_do_token_e_projeta_o_resumo()
    {
        // Arrange
        var funil = new LinhaFunilObrigacoes(
            Total: 100,
            AlcancaramNotificada: 80,
            AlcancaramRespondida: 60,
            AlcancaramAgendada: 45,
            Cumpridas: 30,
            Perdidas: 15,
            ValorRecuperado: 12600m,
            ValorPerdido: 6300m,
            MesInicio: new DateTime(2025, 3, 1),
            MesFim: new DateTime(2026, 9, 1));

        _repositorio
            .Setup(r => r.ObterFunilDaClinicaAsync(IdClinicaDoToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(funil);

        var casoDeUso = new ObterResumoObrigacoes(_repositorio.Object, _tenant.Object);

        // Act
        var dto = await casoDeUso.ExecutarAsync();

        // Assert
        Assert.Equal(IdClinicaDoToken, dto.IdClinica);
        Assert.Equal(100, dto.Total);
        Assert.Equal(30, dto.Cumpridas);
        Assert.Equal(15, dto.PararamEmAgendada);
        Assert.Equal(15, dto.PararamEmRespondida);
        Assert.Equal(20, dto.PararamEmNotificada);
        Assert.Equal(15, dto.Perdidas);
        Assert.Equal(5, dto.NaoTrabalhadas);
        Assert.Equal(12600m, dto.ValorRecuperado);
        Assert.Equal(6300m, dto.ValorPerdido);
        Assert.Equal(new DateTime(2025, 3, 1), dto.MesInicio);

        _repositorio.Verify(
            r => r.ObterFunilDaClinicaAsync(IdClinicaDoToken, It.IsAny<CancellationToken>()),
            Times.Once);
        _repositorio.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Clinica_sem_obrigacao_vencida_devolve_resumo_zerado_sem_datas()
    {
        // Arrange
        _repositorio
            .Setup(r => r.ObterFunilDaClinicaAsync(IdClinicaDoToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LinhaFunilObrigacoes(0, 0, 0, 0, 0, 0, 0m, 0m, null, null));

        var casoDeUso = new ObterResumoObrigacoes(_repositorio.Object, _tenant.Object);

        // Act
        var dto = await casoDeUso.ExecutarAsync();

        // Assert
        Assert.Equal(0, dto.Total);
        Assert.Null(dto.MesInicio);
        Assert.Null(dto.MesFim);
    }
}
