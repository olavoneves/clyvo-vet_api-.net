using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Excecoes;
using Clyvo.Insights.Application.Obrigacoes;
using Clyvo.Insights.Application.Paginacao;
using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Domain.Obrigacoes;
using Moq;

namespace Clyvo.Insights.Tests.Unit.Aplicacao.Obrigacoes;

/// <summary>
/// Listagem e detalhe de obrigação. O que se verifica é orquestração: que o
/// tenant sai do token e chega ao repositório, que consulta inválida não vai ao
/// banco e que a linha da view vira o DTO certo.
/// </summary>
public class CasosDeUsoDeObrigacaoTests
{
    private const long IdClinicaDoToken = 23;
    private const long IdClinicaVizinha = 24;

    private readonly Mock<IDetalheObrigacaoRepository> _repositorio = new(MockBehavior.Strict);
    private readonly Mock<ITenantContext> _tenant = new();

    public CasosDeUsoDeObrigacaoTests()
    {
        _tenant.SetupGet(t => t.IdClinica).Returns(IdClinicaDoToken);
    }

    private static LinhaObrigacao Linha(long id, StatusObrigacao status = StatusObrigacao.Cumprida) =>
        new(id, 7, "Thor", "Patrícia", "Vacinação anual", "VACINA", "Dose de reforço",
            status, GrupoCoorte.Controle,
            new DateTime(2026, 3, 10, 0, 0, 0),
            new DateTime(2026, 3, 3),
            new DateTime(2026, 3, 17),
            180m, 175.5m);

    [Fact]
    public async Task ListarObrigacoes_ComConsultaValida_ConsultaORepositorioComAClinicaDoToken()
    {
        // Arrange
        _repositorio
            .Setup(r => r.ListarDaClinicaAsync(
                IdClinicaDoToken, It.IsAny<CriteriosListagemObrigacoes>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long _, CriteriosListagemObrigacoes c, CancellationToken _) =>
                new Pagina<LinhaObrigacao>(new[] { Linha(1), Linha(2) }, c.Pagina, c.Tamanho, 42));

        var casoDeUso = new ListarObrigacoes(_repositorio.Object, _tenant.Object);

        // Act
        var listagem = await casoDeUso.ExecutarAsync(
            new ConsultaObrigacoesRequest { Pagina = 2, Tamanho = 2, Status = "cumprida" });

        // Assert
        Assert.Equal(2, listagem.Pagina.Numero);
        Assert.Equal(42, listagem.Pagina.TotalItens);
        Assert.Equal(new long[] { 1, 2 }, listagem.Pagina.Itens.Select(i => i.Id));
        Assert.Equal(StatusObrigacao.Cumprida, listagem.Criterios.Status);

        _repositorio.Verify(r => r.ListarDaClinicaAsync(
            IdClinicaDoToken,
            It.Is<CriteriosListagemObrigacoes>(c => c.Pagina == 2 && c.Tamanho == 2 && c.Status == StatusObrigacao.Cumprida),
            It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ListarObrigacoes_ComTokenDeOutraClinica_ConsultaAOutraClinica()
    {
        // Arrange — o tenant é o do token, qualquer que seja
        _tenant.SetupGet(t => t.IdClinica).Returns(IdClinicaVizinha);
        _repositorio
            .Setup(r => r.ListarDaClinicaAsync(
                IdClinicaVizinha, It.IsAny<CriteriosListagemObrigacoes>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pagina<LinhaObrigacao>(Array.Empty<LinhaObrigacao>(), 1, 20, 0));

        var casoDeUso = new ListarObrigacoes(_repositorio.Object, _tenant.Object);

        // Act
        await casoDeUso.ExecutarAsync(new ConsultaObrigacoesRequest());

        // Assert
        _repositorio.Verify(r => r.ListarDaClinicaAsync(
            IdClinicaDoToken, It.IsAny<CriteriosListagemObrigacoes>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListarObrigacoes_ComConsultaInvalida_RecusaSemIrAoBanco()
    {
        // Arrange
        var casoDeUso = new ListarObrigacoes(_repositorio.Object, _tenant.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ConsultaInvalidaException>(
            () => casoDeUso.ExecutarAsync(new ConsultaObrigacoesRequest { Tamanho = 1000 }));

        _repositorio.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ObterObrigacao_DaClinicaDoToken_ProjetaOsCamposDaView()
    {
        // Arrange
        _repositorio
            .Setup(r => r.ObterDaClinicaAsync(IdClinicaDoToken, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Linha(99, StatusObrigacao.Agendada));

        var casoDeUso = new ObterObrigacao(_repositorio.Object, _tenant.Object);

        // Act
        var dto = await casoDeUso.ExecutarAsync(99);

        // Assert
        Assert.Equal(99, dto.Id);
        Assert.Equal(7, dto.IdPet);
        Assert.Equal("Thor", dto.NomePet);
        Assert.Equal("Patrícia", dto.NomeTutor);
        Assert.Equal("Vacinação anual", dto.Protocolo);
        Assert.Equal("VACINA", dto.Categoria);
        Assert.Equal("Dose de reforço", dto.Etapa);
        Assert.Equal("AGENDADA", dto.Status);
        Assert.Equal("CONTROLE", dto.Grupo);
        Assert.Equal(new DateOnly(2026, 3, 10), dto.DataPrevista);
        Assert.Equal(new DateOnly(2026, 3, 3), dto.JanelaInicio);
        Assert.Equal(new DateOnly(2026, 3, 17), dto.JanelaFim);
        Assert.Equal(180m, dto.ValorEstimado);
        Assert.Equal(175.5m, dto.ValorRealizado);
    }

    [Fact]
    public async Task ObterObrigacao_DeOutraClinica_LancaRecursoNaoEncontrado()
    {
        // Arrange — o repositório recorta por clínica; com o tenant do token a
        // obrigação da vizinha simplesmente não existe
        _repositorio
            .Setup(r => r.ObterDaClinicaAsync(IdClinicaDoToken, 9001, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LinhaObrigacao?)null);

        var casoDeUso = new ObterObrigacao(_repositorio.Object, _tenant.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => casoDeUso.ExecutarAsync(9001));

        _repositorio.Verify(
            r => r.ObterDaClinicaAsync(IdClinicaDoToken, 9001, It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.VerifyNoOtherCalls();
    }
}
