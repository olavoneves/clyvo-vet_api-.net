using Clyvo.Insights.Application.Excecoes;
using Clyvo.Insights.Application.Obrigacoes;
using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Domain.Obrigacoes;

namespace Clyvo.Insights.Tests.Unit.Aplicacao.Obrigacoes;

public class CriteriosListagemObrigacoesTests
{
    private static ConsultaInvalidaException Recusa(ConsultaObrigacoesRequest consulta) =>
        Assert.Throws<ConsultaInvalidaException>(() => CriteriosListagemObrigacoes.APartirDe(consulta));

    [Fact]
    public void APartirDe_SemParametros_AssumeOsPadroes()
    {
        // Act
        var criterios = CriteriosListagemObrigacoes.APartirDe(new ConsultaObrigacoesRequest());

        // Assert
        Assert.Equal(1, criterios.Pagina);
        Assert.Equal(20, criterios.Tamanho);
        Assert.Equal(0, criterios.Deslocamento);
        Assert.Equal(CampoOrdenacaoObrigacoes.DataPrevista, criterios.OrdenarPor);
        Assert.Equal(DirecaoOrdenacao.Asc, criterios.Direcao);
        Assert.Null(criterios.Status);
        Assert.Null(criterios.Grupo);
        Assert.Null(criterios.Categoria);
        Assert.Null(criterios.De);
        Assert.Null(criterios.Ate);
        Assert.Null(criterios.IdPet);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void APartirDe_ComPaginaMenorQueUm_RecusaComErroEmPagina(int pagina)
    {
        // Act
        var excecao = Recusa(new ConsultaObrigacoesRequest { Pagina = pagina });

        // Assert
        Assert.Contains("pagina", excecao.Erros.Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public void APartirDe_ComTamanhoForaDeUmACem_RecusaComErroEmTamanho(int tamanho)
    {
        // Act
        var excecao = Recusa(new ConsultaObrigacoesRequest { Tamanho = tamanho });

        // Assert
        Assert.Contains("tamanho", excecao.Erros.Keys);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void APartirDe_ComTamanhoNoLimiteDaFaixa_Aceita(int tamanho)
    {
        // Act
        var criterios = CriteriosListagemObrigacoes.APartirDe(new ConsultaObrigacoesRequest { Tamanho = tamanho });

        // Assert
        Assert.Equal(tamanho, criterios.Tamanho);
    }

    [Fact]
    public void APartirDe_ComPaginaCujoDeslocamentoNaoCabeNumInt_Recusa()
    {
        // Act
        var excecao = Recusa(new ConsultaObrigacoesRequest { Pagina = int.MaxValue, Tamanho = 100 });

        // Assert
        Assert.Contains("pagina", excecao.Erros.Keys);
    }

    [Fact]
    public void Deslocamento_NaTerceiraPaginaDeDez_PulaVinte()
    {
        // Act
        var criterios = CriteriosListagemObrigacoes.APartirDe(
            new ConsultaObrigacoesRequest { Pagina = 3, Tamanho = 10 });

        // Assert
        Assert.Equal(20, criterios.Deslocamento);
    }

    [Theory]
    [InlineData("nomeTutor")]
    [InlineData("ID_OBRIGACAO")]
    [InlineData("dataPrevista; DROP TABLE x")]
    public void APartirDe_ComCampoDeOrdenacaoForaDaListaBranca_Recusa(string campo)
    {
        // Act
        var excecao = Recusa(new ConsultaObrigacoesRequest { OrdenarPor = campo });

        // Assert
        Assert.Contains("ordenarPor", excecao.Erros.Keys);
    }

    [Theory]
    [InlineData("dataPrevista", CampoOrdenacaoObrigacoes.DataPrevista)]
    [InlineData("STATUS", CampoOrdenacaoObrigacoes.Status)]
    [InlineData("pet", CampoOrdenacaoObrigacoes.Pet)]
    [InlineData("valorestimado", CampoOrdenacaoObrigacoes.ValorEstimado)]
    public void APartirDe_ComCampoDaListaBranca_IgnoraCaixa(string campo, CampoOrdenacaoObrigacoes esperado)
    {
        // Act
        var criterios = CriteriosListagemObrigacoes.APartirDe(new ConsultaObrigacoesRequest { OrdenarPor = campo });

        // Assert
        Assert.Equal(esperado, criterios.OrdenarPor);
    }

    [Theory]
    [InlineData("desc", DirecaoOrdenacao.Desc)]
    [InlineData("ASC", DirecaoOrdenacao.Asc)]
    public void APartirDe_ComDirecaoValida_Interpreta(string direcao, DirecaoOrdenacao esperada)
    {
        // Act
        var criterios = CriteriosListagemObrigacoes.APartirDe(new ConsultaObrigacoesRequest { Direcao = direcao });

        // Assert
        Assert.Equal(esperada, criterios.Direcao);
    }

    [Fact]
    public void APartirDe_ComDirecaoDesconhecida_Recusa()
    {
        // Act
        var excecao = Recusa(new ConsultaObrigacoesRequest { Direcao = "decrescente" });

        // Assert
        Assert.Contains("direcao", excecao.Erros.Keys);
    }

    [Theory]
    [InlineData("tratamento", GrupoCoorte.Tratado)]
    [InlineData("CONTROLE", GrupoCoorte.Controle)]
    public void APartirDe_ComGrupoValido_TraduzParaOBracoDoDominio(string grupo, GrupoCoorte esperado)
    {
        // Act
        var criterios = CriteriosListagemObrigacoes.APartirDe(new ConsultaObrigacoesRequest { Grupo = grupo });

        // Assert
        Assert.Equal(esperado, criterios.Grupo);
    }

    [Fact]
    public void APartirDe_ComGrupoDesconhecido_Recusa()
    {
        // Act — "tratado" é o rótulo da resposta, não o valor do filtro
        var excecao = Recusa(new ConsultaObrigacoesRequest { Grupo = "tratado" });

        // Assert
        Assert.Contains("grupo", excecao.Erros.Keys);
    }

    [Fact]
    public void APartirDe_ComStatusDesconhecido_RecusaListandoOsValidos()
    {
        // Act
        var excecao = Recusa(new ConsultaObrigacoesRequest { Status = "ATRASADA" });

        // Assert
        var mensagem = Assert.Single(excecao.Erros["status"]);
        Assert.Contains("CUMPRIDA", mensagem);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("UMA-CATEGORIA-COM-MAIS-DE-VINTE")]
    public void APartirDe_ComCategoriaVaziaOuLongaDemais_Recusa(string categoria)
    {
        // Act
        var excecao = Recusa(new ConsultaObrigacoesRequest { Categoria = categoria });

        // Assert
        Assert.Contains("categoria", excecao.Erros.Keys);
    }

    [Fact]
    public void APartirDe_ComPeriodoInvertido_Recusa()
    {
        // Act
        var excecao = Recusa(new ConsultaObrigacoesRequest
        {
            De = new DateOnly(2026, 2, 1),
            Ate = new DateOnly(2026, 1, 1)
        });

        // Assert
        Assert.Contains("de", excecao.Erros.Keys);
    }

    [Fact]
    public void APartirDe_ComPeriodoDeUmDiaSo_Aceita()
    {
        // Arrange
        var dia = new DateOnly(2026, 1, 15);

        // Act
        var criterios = CriteriosListagemObrigacoes.APartirDe(new ConsultaObrigacoesRequest { De = dia, Ate = dia });

        // Assert
        Assert.Equal(dia, criterios.De);
        Assert.Equal(dia, criterios.Ate);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-3L)]
    public void APartirDe_ComIdDePetNaoPositivo_Recusa(long pet)
    {
        // Act
        var excecao = Recusa(new ConsultaObrigacoesRequest { Pet = pet });

        // Assert
        Assert.Contains("pet", excecao.Erros.Keys);
    }

    [Fact]
    public void APartirDe_ComVariosParametrosInvalidos_DevolveTodosOsErrosDeUmaVez()
    {
        // Act
        var excecao = Recusa(new ConsultaObrigacoesRequest
        {
            Pagina = 0,
            Tamanho = 500,
            OrdenarPor = "x",
            Status = "y"
        });

        // Assert
        Assert.Equal(new[] { "ordenarPor", "pagina", "status", "tamanho" }, excecao.Erros.Keys.Order());
    }

    [Fact]
    public void APartirDe_ComTodosOsFiltrosCombinados_InterpretaCadaUm()
    {
        // Act
        var criterios = CriteriosListagemObrigacoes.APartirDe(new ConsultaObrigacoesRequest
        {
            Pagina = 2,
            Tamanho = 50,
            Status = "agendada",
            Grupo = "controle",
            Categoria = " vacina ",
            De = new DateOnly(2026, 1, 1),
            Ate = new DateOnly(2026, 3, 31),
            Pet = 42,
            OrdenarPor = "valorEstimado",
            Direcao = "desc"
        });

        // Assert
        Assert.Equal(2, criterios.Pagina);
        Assert.Equal(50, criterios.Tamanho);
        Assert.Equal(StatusObrigacao.Agendada, criterios.Status);
        Assert.Equal(GrupoCoorte.Controle, criterios.Grupo);
        Assert.Equal("VACINA", criterios.Categoria);
        Assert.Equal(new DateOnly(2026, 1, 1), criterios.De);
        Assert.Equal(new DateOnly(2026, 3, 31), criterios.Ate);
        Assert.Equal(42, criterios.IdPet);
        Assert.Equal(CampoOrdenacaoObrigacoes.ValorEstimado, criterios.OrdenarPor);
        Assert.Equal(DirecaoOrdenacao.Desc, criterios.Direcao);
    }

    [Fact]
    public void ParaParametrosDeConsulta_ComFiltrosCombinados_DevolveAFormaCanonica()
    {
        // Arrange
        var criterios = CriteriosListagemObrigacoes.APartirDe(new ConsultaObrigacoesRequest
        {
            Status = "agendada",
            Grupo = "CONTROLE",
            Categoria = "vacina",
            De = new DateOnly(2026, 1, 1),
            Ate = new DateOnly(2026, 3, 31),
            Pet = 42,
            OrdenarPor = "VALORESTIMADO",
            Direcao = "DESC"
        });

        // Act
        var parametros = criterios.ParaParametrosDeConsulta();

        // Assert
        Assert.Equal(new Dictionary<string, string>
        {
            ["pagina"] = "1",
            ["tamanho"] = "20",
            ["status"] = "AGENDADA",
            ["grupo"] = "controle",
            ["categoria"] = "VACINA",
            ["de"] = "2026-01-01",
            ["ate"] = "2026-03-31",
            ["pet"] = "42",
            ["ordenarPor"] = "valorEstimado",
            ["direcao"] = "desc"
        }, parametros);
    }

    [Fact]
    public void ParaParametrosDeConsulta_SemFiltros_LevaSoPaginacaoEOrdenacao()
    {
        // Arrange
        var criterios = CriteriosListagemObrigacoes.APartirDe(new ConsultaObrigacoesRequest());

        // Act
        var parametros = criterios.ParaParametrosDeConsulta();

        // Assert
        Assert.Equal(new[] { "direcao", "ordenarPor", "pagina", "tamanho" }, parametros.Keys.Order());
    }

    [Fact]
    public void NaPagina_ComOutraPagina_PreservaFiltrosEOrdenacao()
    {
        // Arrange
        var criterios = CriteriosListagemObrigacoes.APartirDe(new ConsultaObrigacoesRequest
        {
            Status = "PERDIDA",
            Grupo = "tratamento",
            Tamanho = 10,
            OrdenarPor = "pet",
            Direcao = "desc"
        });

        // Act
        var seguinte = criterios.NaPagina(4);

        // Assert — só a página muda
        var antes = criterios.ParaParametrosDeConsulta();
        var depois = seguinte.ParaParametrosDeConsulta();

        Assert.Equal("4", depois["pagina"]);
        Assert.Equal(antes.Where(p => p.Key != "pagina"), depois.Where(p => p.Key != "pagina"));
        Assert.Equal(30, seguinte.Deslocamento);
    }

    [Fact]
    public void NaPagina_ComPaginaZero_Lanca()
    {
        // Arrange
        var criterios = CriteriosListagemObrigacoes.APartirDe(new ConsultaObrigacoesRequest());

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => criterios.NaPagina(0));
    }

    [Fact]
    public void APartirDe_ComOsParametrosCanonicosDeVolta_ReproduzOsMesmosCriterios()
    {
        // Arrange — a ida e volta é o que faz o link de página preservar a consulta
        var original = CriteriosListagemObrigacoes.APartirDe(new ConsultaObrigacoesRequest
        {
            Status = "cumprida",
            Grupo = "controle",
            De = new DateOnly(2026, 1, 1),
            OrdenarPor = "status",
            Direcao = "desc"
        });
        var p = original.ParaParametrosDeConsulta();

        // Act
        var relido = CriteriosListagemObrigacoes.APartirDe(new ConsultaObrigacoesRequest
        {
            Pagina = int.Parse(p["pagina"]),
            Tamanho = int.Parse(p["tamanho"]),
            Status = p["status"],
            Grupo = p["grupo"],
            De = DateOnly.Parse(p["de"]),
            OrdenarPor = p["ordenarPor"],
            Direcao = p["direcao"]
        });

        // Assert
        Assert.Equal(p, relido.ParaParametrosDeConsulta());
    }

    [Fact]
    public void TextoDoGrupo_DeCadaBraco_DevolveOValorDoFiltro()
    {
        // Act & Assert
        Assert.Equal("tratamento", CriteriosListagemObrigacoes.TextoDoGrupo(GrupoCoorte.Tratado));
        Assert.Equal("controle", CriteriosListagemObrigacoes.TextoDoGrupo(GrupoCoorte.Controle));
        Assert.Throws<ArgumentOutOfRangeException>(() => CriteriosListagemObrigacoes.TextoDoGrupo((GrupoCoorte)99));
    }

    [Fact]
    public void APartirDe_ComConsultaNula_Lanca()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CriteriosListagemObrigacoes.APartirDe(null!));
    }
}
