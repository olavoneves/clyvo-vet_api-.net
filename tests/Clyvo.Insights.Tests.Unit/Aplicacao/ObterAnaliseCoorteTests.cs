using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Domain.Metas;
using Clyvo.Insights.Application.Projecoes;
using Clyvo.Insights.Domain.Projecoes;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Clyvo.Insights.Tests.Unit.Aplicacao;

/// <summary>
/// Verifica orquestração e mapeamento. A aritmética já é coberta em
/// <c>AnaliseCoorteTests</c> — recalculá-la aqui criaria uma segunda
/// implementação da mesma regra dentro da própria suíte.
/// </summary>
public class ObterAnaliseCoorteTests
{
    private const long IdClinicaDoToken = 23;

    private readonly Mock<ILeituraDoCoreRepository> _repositorio = new(MockBehavior.Strict);
    private readonly Mock<IMetaIndicadorRepository> _metas = new();
    private readonly Mock<IProjecaoRepository> _projecoes = new();
    private readonly Mock<ITenantContext> _tenant = new();

    public ObterAnaliseCoorteTests()
    {
        _tenant.SetupGet(t => t.IdClinica).Returns(IdClinicaDoToken);

        _metas
            .Setup(m => m.ListarDaClinicaAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MetaIndicador>());
    }

    private ObterAnaliseCoorte CasoDeUso() => new(
        _repositorio.Object,
        _metas.Object,
        _projecoes.Object,
        _tenant.Object,
        NullLogger<ObterAnaliseCoorte>.Instance);

    private void ConfigurarMetas(params MetaIndicador[] metas) =>
        _metas
            .Setup(m => m.ListarDaClinicaAsync(IdClinicaDoToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metas);

    private void ConfigurarLeitura(long idClinica, params LinhaCoorte[] linhas) =>
        _repositorio
            .Setup(r => r.ObterCoortesDaClinicaAsync(idClinica, It.IsAny<CancellationToken>()))
            .ReturnsAsync(linhas);

    [Fact]
    public async Task Consulta_o_repositorio_com_a_clinica_do_token()
    {
        // Arrange
        ConfigurarLeitura(IdClinicaDoToken,
            new LinhaCoorte(GrupoCoorte.Tratado, 2715, 1601, 218, 203.73m),
            new LinhaCoorte(GrupoCoorte.Controle, 354, 120, 27, 203.73m));

        // Act
        var dto = await CasoDeUso().ExecutarAsync();

        // Assert
        _repositorio.Verify(
            r => r.ObterCoortesDaClinicaAsync(IdClinicaDoToken, It.IsAny<CancellationToken>()),
            Times.Once);
        _repositorio.VerifyNoOtherCalls();
        Assert.Equal(IdClinicaDoToken, dto.IdClinica);
    }

    [Fact]
    public async Task Mapeia_as_linhas_da_view_para_o_dto_de_resposta()
    {
        // Arrange
        ConfigurarLeitura(IdClinicaDoToken,
            new LinhaCoorte(GrupoCoorte.Tratado, 2715, 1601, 218, 203.73m),
            new LinhaCoorte(GrupoCoorte.Controle, 354, 120, 27, 203.73m));

        // Act
        var dto = await CasoDeUso().ExecutarAsync();

        // Assert
        Assert.True(dto.Disponivel);
        Assert.Null(dto.MotivoIndisponibilidade);
        Assert.Equal(203.73m, dto.TicketMedio);

        Assert.Equal("TRATADO", dto.Tratado.Grupo);
        Assert.Equal(2715, dto.Tratado.ObrigacoesResolvidas);
        Assert.Equal(1601, dto.Tratado.ObrigacoesCumpridas);
        Assert.Equal(218, dto.Tratado.PetsDistintos);
        Assert.Equal(58.97m, dto.Tratado.TaxaCumprimentoPercentual);

        Assert.Equal("CONTROLE", dto.Controle.Grupo);
        Assert.Equal(354, dto.Controle.ObrigacoesResolvidas);
        Assert.Equal(33.90m, dto.Controle.TaxaCumprimentoPercentual);
    }

    [Fact]
    public async Task Repassa_ao_dto_os_numeros_que_o_dominio_calculou()
    {
        // Arrange
        ConfigurarLeitura(IdClinicaDoToken,
            new LinhaCoorte(GrupoCoorte.Tratado, 1000, 600, 100, 200m),
            new LinhaCoorte(GrupoCoorte.Controle, 100, 40, 10, 200m));

        var esperado = AnaliseCoorte.Calcular(
            Coorte.Criar(GrupoCoorte.Tratado, 1000, 600, 100),
            Coorte.Criar(GrupoCoorte.Controle, 100, 40, 10),
            200m);

        // Act
        var dto = await CasoDeUso().ExecutarAsync();

        // Assert
        Assert.Equal(esperado.DeltaPontosPercentuais, dto.DeltaPontosPercentuais);
        Assert.Equal(esperado.ConsultasAtribuiveis, dto.ConsultasAtribuiveis);
        Assert.Equal(esperado.ReceitaRecuperada, dto.ReceitaRecuperada);
        Assert.Equal(esperado.ReceitaEstimavel, dto.ReceitaEstimavel);
    }

    [Fact]
    public async Task Grupo_ausente_na_leitura_vira_coorte_vazia_e_analise_indisponivel()
    {
        // Arrange — clínica cujo controle ainda não teve obrigação resolvida
        ConfigurarLeitura(IdClinicaDoToken,
            new LinhaCoorte(GrupoCoorte.Tratado, 2715, 1601, 218, 203.73m));

        // Act
        var dto = await CasoDeUso().ExecutarAsync();

        // Assert
        Assert.False(dto.Disponivel);
        Assert.NotNull(dto.MotivoIndisponibilidade);
        Assert.Equal(0, dto.Controle.ObrigacoesResolvidas);
        Assert.Equal(2715, dto.Tratado.ObrigacoesResolvidas);
    }

    [Fact]
    public async Task Leitura_vazia_devolve_analise_indisponivel_sem_estourar()
    {
        // Arrange
        ConfigurarLeitura(IdClinicaDoToken);

        // Act
        var dto = await CasoDeUso().ExecutarAsync();

        // Assert
        Assert.False(dto.Disponivel);
        Assert.Null(dto.TicketMedio);
        Assert.Equal(0m, dto.ReceitaRecuperada);
    }

    [Fact]
    public async Task Ticket_medio_nulo_nas_linhas_chega_nulo_ao_dto()
    {
        // Arrange — clínica sem consulta realizada com valor lançado
        ConfigurarLeitura(IdClinicaDoToken,
            new LinhaCoorte(GrupoCoorte.Tratado, 1000, 600, 100, null),
            new LinhaCoorte(GrupoCoorte.Controle, 100, 40, 10, null));

        // Act
        var dto = await CasoDeUso().ExecutarAsync();

        // Assert
        Assert.True(dto.Disponivel);
        Assert.Null(dto.TicketMedio);
        Assert.False(dto.ReceitaEstimavel);
        Assert.Equal(0m, dto.ReceitaRecuperada);
        Assert.Equal(20.00m, dto.DeltaPontosPercentuais);
    }

    [Fact]
    public async Task Propaga_o_cancellation_token_ate_o_repositorio()
    {
        // Arrange
        using var origem = new CancellationTokenSource();
        _repositorio
            .Setup(r => r.ObterCoortesDaClinicaAsync(IdClinicaDoToken, origem.Token))
            .ReturnsAsync(Array.Empty<LinhaCoorte>());

        // Act
        await CasoDeUso().ExecutarAsync(origem.Token);

        // Assert
        _repositorio.Verify(
            r => r.ObterCoortesDaClinicaAsync(IdClinicaDoToken, origem.Token),
            Times.Once);
    }

    [Fact]
    public async Task Confronta_as_metas_da_clinica_com_o_valor_apurado()
    {
        // Arrange
        ConfigurarLeitura(IdClinicaDoToken,
            new LinhaCoorte(GrupoCoorte.Tratado, 1000, 600, 100, 200m),    // taxa 60%
            new LinhaCoorte(GrupoCoorte.Controle, 100, 40, 10, 200m));     // delta 20 p.p.

        ConfigurarMetas(
            MetaIndicador.Criar(IdClinicaDoToken, IndicadorMonitorado.TaxaCumprimento, 55m, DateTime.UtcNow),
            MetaIndicador.Criar(IdClinicaDoToken, IndicadorMonitorado.DeltaPontosPercentuais, 25m, DateTime.UtcNow));

        // Act
        var dto = await CasoDeUso().ExecutarAsync();

        // Assert
        var taxa = Assert.Single(dto.Metas, m => m.Indicador == nameof(IndicadorMonitorado.TaxaCumprimento));
        Assert.Equal(60.00m, taxa.ValorApurado);
        Assert.False(taxa.AbaixoDoLimiar);

        var delta = Assert.Single(dto.Metas, m => m.Indicador == nameof(IndicadorMonitorado.DeltaPontosPercentuais));
        Assert.Equal(20.00m, delta.ValorApurado);
        Assert.True(delta.AbaixoDoLimiar);
    }

    [Fact]
    public async Task Analise_indisponivel_nao_sinaliza_meta_para_nao_dar_alarme_falso()
    {
        // Arrange
        ConfigurarLeitura(IdClinicaDoToken);
        ConfigurarMetas(
            MetaIndicador.Criar(IdClinicaDoToken, IndicadorMonitorado.TaxaCumprimento, 55m, DateTime.UtcNow));

        // Act
        var dto = await CasoDeUso().ExecutarAsync();

        // Assert
        Assert.False(dto.Disponivel);
        Assert.Empty(dto.Metas);
        _metas.Verify(
            m => m.ListarDaClinicaAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Registra_a_consulta_e_o_snapshot_do_dia()
    {
        // Arrange
        ConfigurarLeitura(IdClinicaDoToken,
            new LinhaCoorte(GrupoCoorte.Tratado, 1000, 600, 100, 200m),
            new LinhaCoorte(GrupoCoorte.Controle, 100, 40, 10, 200m));

        _projecoes
            .Setup(p => p.ObterSnapshotDoDiaAsync(IdClinicaDoToken, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SnapshotCoorte?)null);

        SnapshotCoorte? gravado = null;
        _projecoes
            .Setup(p => p.RegistrarSnapshotAsync(It.IsAny<SnapshotCoorte>(), It.IsAny<AnaliseCoorteDto>(), It.IsAny<CancellationToken>()))
            .Callback<SnapshotCoorte, AnaliseCoorteDto, CancellationToken>((s, _, _) => gravado = s)
            .Returns(Task.CompletedTask);

        // Act
        var dto = await CasoDeUso().ExecutarAsync();

        // Assert
        Assert.NotNull(gravado);
        Assert.Equal(IdClinicaDoToken, gravado!.IdClinica);
        Assert.Equal(dto.DeltaPontosPercentuais, gravado.DeltaPontosPercentuais);
        Assert.Equal(1000, gravado.ObrigacoesResolvidasTratado);
        _projecoes.Verify(
            p => p.RegistrarConsultaAsync(It.Is<RegistroDeConsulta>(r => r.IdClinica == IdClinicaDoToken && r.Disponivel), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Reapura_o_snapshot_do_dia_em_vez_de_criar_outro_ponto()
    {
        // Arrange
        ConfigurarLeitura(IdClinicaDoToken,
            new LinhaCoorte(GrupoCoorte.Tratado, 1000, 600, 100, 200m),
            new LinhaCoorte(GrupoCoorte.Controle, 100, 40, 10, 200m));

        var jaExistente = SnapshotCoorte.De(
            IdClinicaDoToken,
            AnaliseCoorte.Calcular(
                Coorte.Criar(GrupoCoorte.Tratado, 900, 500, 90),
                Coorte.Criar(GrupoCoorte.Controle, 90, 40, 9),
                200m),
            DateOnly.FromDateTime(DateTime.UtcNow));

        _projecoes
            .Setup(p => p.ObterSnapshotDoDiaAsync(IdClinicaDoToken, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jaExistente);

        // Act
        var dto = await CasoDeUso().ExecutarAsync();

        // Assert
        Assert.Equal(dto.DeltaPontosPercentuais, jaExistente.DeltaPontosPercentuais);
        Assert.Equal(1000, jaExistente.ObrigacoesResolvidasTratado);
        _projecoes.Verify(
            p => p.RegistrarSnapshotAsync(jaExistente, It.IsAny<AnaliseCoorteDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Analise_indisponivel_registra_a_consulta_mas_nao_grava_snapshot()
    {
        // Arrange
        ConfigurarLeitura(IdClinicaDoToken);

        // Act
        var dto = await CasoDeUso().ExecutarAsync();

        // Assert
        Assert.False(dto.Disponivel);
        _projecoes.Verify(
            p => p.RegistrarConsultaAsync(It.Is<RegistroDeConsulta>(r => !r.Disponivel), It.IsAny<CancellationToken>()),
            Times.Once);
        _projecoes.Verify(
            p => p.RegistrarSnapshotAsync(It.IsAny<SnapshotCoorte>(), It.IsAny<AnaliseCoorteDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Mongo_fora_do_ar_nao_derruba_a_analise()
    {
        // Arrange
        ConfigurarLeitura(IdClinicaDoToken,
            new LinhaCoorte(GrupoCoorte.Tratado, 1000, 600, 100, 200m),
            new LinhaCoorte(GrupoCoorte.Controle, 100, 40, 10, 200m));

        _projecoes
            .Setup(p => p.RegistrarConsultaAsync(It.IsAny<RegistroDeConsulta>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("servidor de projeção indisponível"));

        // Act
        var dto = await CasoDeUso().ExecutarAsync();

        // Assert
        Assert.True(dto.Disponivel);
        Assert.Equal(20.00m, dto.DeltaPontosPercentuais);
    }
}
