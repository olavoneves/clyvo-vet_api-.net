using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Domain.Excecoes;

namespace Clyvo.Insights.Tests.Unit.Dominio;

public class AnaliseCoorteTests
{
    /// <summary>
    /// Números da clínica 23 no banco de demonstração: 2715 obrigações
    /// resolvidas no tratado com 1601 cumpridas (58,97%) contra 354 resolvidas
    /// no controle com 120 cumpridas (33,90%).
    /// </summary>
    private static Coorte TratadoCanonico() =>
        Coorte.Criar(GrupoCoorte.Tratado, obrigacoesResolvidas: 2715, obrigacoesCumpridas: 1601, petsDistintos: 218);

    private static Coorte ControleCanonico() =>
        Coorte.Criar(GrupoCoorte.Controle, obrigacoesResolvidas: 354, obrigacoesCumpridas: 120, petsDistintos: 27);

    private const decimal TicketCanonico = 203.73m;

    [Fact]
    public void Calcular_ComDeltaPositivo_RetornaConsultasAtribuiveisEReceita()
    {
        // Arrange
        var tratado = TratadoCanonico();
        var controle = ControleCanonico();

        // Act
        var analise = AnaliseCoorte.Calcular(tratado, controle, TicketCanonico);

        // Assert
        Assert.True(analise.Disponivel);
        Assert.Equal(25.07m, analise.DeltaPontosPercentuais);
        Assert.Equal(680.66m, analise.ConsultasAtribuiveis);
        Assert.Equal(138671.07m, analise.ReceitaRecuperada);
        Assert.True(analise.ReceitaEstimavel);
    }

    [Fact]
    public void Calcular_ComDeltaConhecido_MultiplicaResolvidasDoTratadoPeloDelta()
    {
        // Arrange
        var tratado = Coorte.Criar(GrupoCoorte.Tratado, 1000, 600, 100);   // 60%
        var controle = Coorte.Criar(GrupoCoorte.Controle, 100, 40, 10);    // 40%

        // Act
        var analise = AnaliseCoorte.Calcular(tratado, controle, ticketMedio: 200m);

        // Assert
        Assert.Equal(20.00m, analise.DeltaPontosPercentuais);
        Assert.Equal(200.00m, analise.ConsultasAtribuiveis);
        Assert.Equal(40000.00m, analise.ReceitaRecuperada);
    }

    [Fact]
    public void Calcular_ComCoorteDeControleVazia_RetornaAnaliseIndisponivel()
    {
        // Arrange
        var tratado = TratadoCanonico();
        var controleSemDados = Coorte.Criar(GrupoCoorte.Controle, 0, 0, 0);

        // Act
        var analise = AnaliseCoorte.Calcular(tratado, controleSemDados, TicketCanonico);

        // Assert
        Assert.False(analise.Disponivel);
        Assert.NotNull(analise.MotivoIndisponibilidade);
        Assert.Equal(0m, analise.DeltaPontosPercentuais);
        Assert.Equal(0m, analise.ConsultasAtribuiveis);
        Assert.Equal(0m, analise.ReceitaRecuperada);
        Assert.False(analise.ReceitaEstimavel);
    }

    [Fact]
    public void Calcular_ComCoorteTratadaVazia_RetornaAnaliseIndisponivel()
    {
        // Arrange
        var tratadoSemDados = Coorte.Criar(GrupoCoorte.Tratado, 0, 0, 0);
        var controle = ControleCanonico();

        // Act
        var analise = AnaliseCoorte.Calcular(tratadoSemDados, controle, TicketCanonico);

        // Assert
        Assert.False(analise.Disponivel);
        Assert.NotNull(analise.MotivoIndisponibilidade);
        Assert.Equal(0m, analise.ConsultasAtribuiveis);
    }

    [Fact]
    public void Calcular_ComTratadoPiorQueControle_PreservaDeltaNegativo()
    {
        // Arrange — o produto performou pior que a inércia
        var tratado = Coorte.Criar(GrupoCoorte.Tratado, 1000, 300, 100);   // 30%
        var controle = Coorte.Criar(GrupoCoorte.Controle, 100, 50, 10);    // 50%

        // Act
        var analise = AnaliseCoorte.Calcular(tratado, controle, ticketMedio: 200m);

        // Assert
        Assert.True(analise.Disponivel);
        Assert.Equal(-20.00m, analise.DeltaPontosPercentuais);
        Assert.Equal(-200.00m, analise.ConsultasAtribuiveis);
        Assert.Equal(-40000.00m, analise.ReceitaRecuperada);
    }

    [Fact]
    public void Calcular_ComTaxasIguais_RetornaDeltaZeroEAnaliseDisponivel()
    {
        // Arrange
        var tratado = Coorte.Criar(GrupoCoorte.Tratado, 800, 400, 80);     // 50%
        var controle = Coorte.Criar(GrupoCoorte.Controle, 80, 40, 8);      // 50%

        // Act
        var analise = AnaliseCoorte.Calcular(tratado, controle, ticketMedio: 300m);

        // Assert
        Assert.True(analise.Disponivel);
        Assert.Equal(0m, analise.DeltaPontosPercentuais);
        Assert.Equal(0m, analise.ConsultasAtribuiveis);
        Assert.Equal(0m, analise.ReceitaRecuperada);
    }

    [Fact]
    public void Calcular_ComTratadoEmCemPorCento_AtribuiComplementoDaTaxaDeControle()
    {
        // Arrange
        var tratado = Coorte.Criar(GrupoCoorte.Tratado, 500, 500, 60);     // 100%
        var controle = Coorte.Criar(GrupoCoorte.Controle, 100, 35, 12);    // 35%

        // Act
        var analise = AnaliseCoorte.Calcular(tratado, controle, ticketMedio: 100m);

        // Assert
        Assert.Equal(65.00m, analise.DeltaPontosPercentuais);
        Assert.Equal(325.00m, analise.ConsultasAtribuiveis);
        Assert.Equal(32500.00m, analise.ReceitaRecuperada);
    }

    [Fact]
    public void Calcular_ComTicketMedioAusente_ZeraReceitaEMantemDelta()
    {
        // Arrange
        var tratado = TratadoCanonico();
        var controle = ControleCanonico();

        // Act
        var analise = AnaliseCoorte.Calcular(tratado, controle, ticketMedio: null);

        // Assert
        Assert.True(analise.Disponivel);
        Assert.Equal(25.07m, analise.DeltaPontosPercentuais);
        Assert.Equal(680.66m, analise.ConsultasAtribuiveis);
        Assert.Equal(0m, analise.ReceitaRecuperada);
        Assert.False(analise.ReceitaEstimavel);
    }

    [Fact]
    public void Calcular_ComTicketMedioZerado_ZeraReceitaEMantemDelta()
    {
        // Arrange
        var tratado = TratadoCanonico();
        var controle = ControleCanonico();

        // Act
        var analise = AnaliseCoorte.Calcular(tratado, controle, ticketMedio: 0m);

        // Assert
        Assert.True(analise.Disponivel);
        Assert.Equal(25.07m, analise.DeltaPontosPercentuais);
        Assert.Equal(0m, analise.ReceitaRecuperada);
        Assert.False(analise.ReceitaEstimavel);
    }

    [Fact]
    public void Calcular_ComTicketMedioNegativo_LancaRegraDeDominio()
    {
        // Arrange
        var tratado = TratadoCanonico();
        var controle = ControleCanonico();

        // Act
        var erro = Record.Exception(() => AnaliseCoorte.Calcular(tratado, controle, ticketMedio: -1m));

        // Assert
        Assert.IsType<RegraDeDominioException>(erro);
    }

    [Fact]
    public void Calcular_ComBracosTrocados_LancaRegraDeDominio()
    {
        // Arrange
        var controle = ControleCanonico();
        var tratado = TratadoCanonico();

        // Act
        var erro = Record.Exception(
            () => AnaliseCoorte.Calcular(tratado: controle, controle: tratado, TicketCanonico));

        // Assert
        Assert.IsType<RegraDeDominioException>(erro);
    }
}
