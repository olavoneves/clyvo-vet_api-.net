using Clyvo.Insights.Api.Hipermidia;
using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Application.Obrigacoes;
using Clyvo.Insights.Application.Paginacao;

namespace Clyvo.Insights.Tests.Unit.Api;

/// <summary>
/// Quais links cada recurso carrega e com quais parâmetros. A conversão de rota
/// em URL é do ASP.NET e fica de fora — o resolvedor falso escreve o nome da
/// rota e os valores, que é exatamente o que se quer conferir.
/// </summary>
public class GeradorDeLinksTests
{
    private readonly GeradorDeLinks _gerador = new(new ResolvedorFalso(), new RelogioFixo(new DateTime(2026, 10, 28)));

    private static ListagemObrigacoesDto Listagem(int pagina, int totalItens, ConsultaObrigacoesRequest? consulta = null)
    {
        var criterios = CriteriosListagemObrigacoes
            .APartirDe(consulta ?? new ConsultaObrigacoesRequest())
            .NaPagina(pagina);

        var itens = Enumerable.Range(1, Math.Max(0, Math.Min(criterios.Tamanho, totalItens - criterios.Deslocamento)))
            .Select(i => Obrigacao(criterios.Deslocamento + i))
            .ToList();

        return new ListagemObrigacoesDto(
            new Pagina<ObrigacaoDto>(itens, pagina, criterios.Tamanho, totalItens), criterios);
    }

    private static ObrigacaoDto Obrigacao(long id) =>
        new(id, 1, "Thor", "Patrícia", "Vacinação", "VACINA", "Dose", "CUMPRIDA", "TRATADO",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 8), 100m, 100m);

    [Fact]
    public void ParaListagemDeObrigacoes_NaPrimeiraPagina_NaoTemPrev()
    {
        // Act
        var pagina = _gerador.ParaListagemDeObrigacoes(Listagem(pagina: 1, totalItens: 45));

        // Assert
        Assert.Equal(new[] { "first", "last", "next", "self" }, pagina.Links.Keys.Order());
        Assert.Contains("pagina=1", pagina.Links["self"].Href);
        Assert.Contains("pagina=2", pagina.Links["next"].Href);
        Assert.Contains("pagina=3", pagina.Links["last"].Href);
        Assert.Equal(3, pagina.TotalPaginas);
    }

    [Fact]
    public void ParaListagemDeObrigacoes_NaPaginaDoMeio_TemTodosOsCinco()
    {
        // Act
        var pagina = _gerador.ParaListagemDeObrigacoes(Listagem(pagina: 2, totalItens: 45));

        // Assert
        Assert.Equal(new[] { "first", "last", "next", "prev", "self" }, pagina.Links.Keys.Order());
        Assert.Contains("pagina=1", pagina.Links["first"].Href);
        Assert.Contains("pagina=1", pagina.Links["prev"].Href);
        Assert.Contains("pagina=2", pagina.Links["self"].Href);
        Assert.Contains("pagina=3", pagina.Links["next"].Href);
    }

    [Fact]
    public void ParaListagemDeObrigacoes_NaUltimaPagina_NaoTemNext()
    {
        // Act
        var pagina = _gerador.ParaListagemDeObrigacoes(Listagem(pagina: 3, totalItens: 45));

        // Assert
        Assert.Equal(new[] { "first", "last", "prev", "self" }, pagina.Links.Keys.Order());
        Assert.Equal(pagina.Links["self"].Href, pagina.Links["last"].Href);
        Assert.Equal(5, pagina.Itens.Count);
    }

    [Fact]
    public void ParaListagemDeObrigacoes_ComPaginaUnica_SoTemSelfFirstELast()
    {
        // Act
        var pagina = _gerador.ParaListagemDeObrigacoes(Listagem(pagina: 1, totalItens: 3));

        // Assert
        Assert.Equal(new[] { "first", "last", "self" }, pagina.Links.Keys.Order());
    }

    [Fact]
    public void ParaListagemDeObrigacoes_ComFiltrosEOrdenacao_PreservaOsEmTodosOsLinksDePagina()
    {
        // Arrange
        var consulta = new ConsultaObrigacoesRequest
        {
            Tamanho = 10,
            Status = "perdida",
            Grupo = "controle",
            Categoria = "exame",
            De = new DateOnly(2026, 1, 1),
            Ate = new DateOnly(2026, 6, 30),
            Pet = 7,
            OrdenarPor = "valorEstimado",
            Direcao = "desc"
        };

        // Act
        var pagina = _gerador.ParaListagemDeObrigacoes(Listagem(pagina: 2, totalItens: 35, consulta));

        // Assert
        foreach (var (relacao, link) in pagina.Links)
        {
            foreach (var esperado in new[]
                     {
                         "tamanho=10", "status=PERDIDA", "grupo=controle", "categoria=EXAME", "de=2026-01-01",
                         "ate=2026-06-30", "pet=7", "ordenarPor=valorEstimado", "direcao=desc"
                     })
            {
                Assert.True(link.Href.Contains(esperado), $"'{relacao}' perdeu {esperado}: {link.Href}");
            }

            Assert.StartsWith($"http://insights.test/{NomesDeRota.ListarObrigacoes}?", link.Href);
            Assert.Null(link.Metodo);
        }
    }

    [Fact]
    public void ParaListagemDeObrigacoes_CadaItem_TemSelfParaODetalhe()
    {
        // Act
        var pagina = _gerador.ParaListagemDeObrigacoes(Listagem(pagina: 1, totalItens: 2));

        // Assert
        Assert.Equal(
            new[] { $"http://insights.test/{NomesDeRota.ObterObrigacao}?id=1", $"http://insights.test/{NomesDeRota.ObterObrigacao}?id=2" },
            pagina.Itens.Select(i => i.Links["self"].Href));
    }

    [Fact]
    public void ParaResumoDeObrigacoes_ComRelogioFixo_TemUmLinkPorBaldeAteOntem()
    {
        // Arrange
        var resumo = new ResumoObrigacoesDto(23, 100, 30, 15, 15, 20, 15, 5, 0m, 0m, null, null);

        // Act
        var recurso = _gerador.ParaResumoDeObrigacoes(resumo);

        // Assert
        Assert.Same(resumo, recurso.Dados);
        Assert.Equal(8, recurso.Links.Count);
        Assert.Equal($"http://insights.test/{NomesDeRota.ObterResumoObrigacoes}", recurso.Links["self"].Href);

        Assert.Contains("status=CUMPRIDA", recurso.Links["cumpridas"].Href);
        Assert.Contains("status=AGENDADA", recurso.Links["pararamEmAgendada"].Href);
        Assert.Contains("status=RESPONDIDA", recurso.Links["pararamEmRespondida"].Href);
        Assert.Contains("status=NOTIFICADA", recurso.Links["pararamEmNotificada"].Href);
        Assert.Contains("status=PERDIDA", recurso.Links["perdidas"].Href);
        Assert.Contains("status=PREVISTA", recurso.Links["naoTrabalhadasPrevistas"].Href);
        Assert.Contains("status=CANCELADA", recurso.Links["naoTrabalhadasCanceladas"].Href);

        Assert.All(recurso.Links.Where(l => l.Key != "self"),
            l => Assert.Contains("ate=2026-10-27", l.Value.Href));
    }

    [Fact]
    public void ParaAnaliseDeCoorte_DeQualquerAnalise_LinkaAListagemDeCadaBraco()
    {
        // Arrange
        var braco = new CoorteDto("TRATADO", 10, 6, 60m, 3);
        var analise = new AnaliseCoorteDto(23, true, null, 20m, 2m, 400m, true, 200m, braco, braco,
            Array.Empty<MetaAvaliadaDto>());

        // Act
        var recurso = _gerador.ParaAnaliseDeCoorte(analise);

        // Assert
        Assert.Equal($"http://insights.test/{NomesDeRota.ObterCoorte}", recurso.Links["self"].Href);
        Assert.Equal($"http://insights.test/{NomesDeRota.ListarObrigacoes}?grupo=tratamento",
            recurso.Links["obrigacoesTratamento"].Href);
        Assert.Equal($"http://insights.test/{NomesDeRota.ListarObrigacoes}?grupo=controle",
            recurso.Links["obrigacoesControle"].Href);
    }

    [Fact]
    public void ParaMeta_DeUmaMeta_TemSelfAtualizarERemoverComOMetodo()
    {
        // Arrange
        var meta = new MetaIndicadorDto(5, "TaxaCumprimento", 55m, DateTime.UtcNow, DateTime.UtcNow);

        // Act
        var recurso = _gerador.ParaMeta(meta);

        // Assert
        Assert.Equal(new Link($"http://insights.test/{NomesDeRota.ObterMeta}?id=5"), recurso.Links["self"]);
        Assert.Equal(new Link($"http://insights.test/{NomesDeRota.AtualizarMeta}?id=5", "PUT"), recurso.Links["atualizar"]);
        Assert.Equal(new Link($"http://insights.test/{NomesDeRota.RemoverMeta}?id=5", "DELETE"), recurso.Links["remover"]);
    }

    [Fact]
    public void ParaMetas_DeUmaColecao_TemSelfECriarComPost()
    {
        // Arrange
        var metas = new[]
        {
            new MetaIndicadorDto(1, "TaxaCumprimento", 55m, DateTime.UtcNow, DateTime.UtcNow),
            new MetaIndicadorDto(2, "DeltaPontosPercentuais", 10m, DateTime.UtcNow, DateTime.UtcNow)
        };

        // Act
        var colecao = _gerador.ParaMetas(metas);

        // Assert
        Assert.Equal(new Link($"http://insights.test/{NomesDeRota.ListarMetas}"), colecao.Links["self"]);
        Assert.Equal(new Link($"http://insights.test/{NomesDeRota.CriarMeta}", "POST"), colecao.Links["criar"]);
        Assert.Equal(2, colecao.Itens.Count);
        Assert.All(colecao.Itens, i => Assert.Equal(3, i.Links.Count));
    }

    /// <summary>Escreve o nome da rota e os valores na ordem em que chegaram.</summary>
    private sealed class ResolvedorFalso : IResolvedorDeRotas
    {
        public string Url(string rota, IReadOnlyDictionary<string, string>? valores = null) =>
            valores is null || valores.Count == 0
                ? $"http://insights.test/{rota}"
                : $"http://insights.test/{rota}?" + string.Join("&", valores.Select(v => $"{v.Key}={v.Value}"));
    }

    private sealed class RelogioFixo : TimeProvider
    {
        private readonly DateTimeOffset _agora;

        public RelogioFixo(DateTime agoraUtc)
        {
            _agora = new DateTimeOffset(agoraUtc, TimeSpan.Zero);
        }

        public override DateTimeOffset GetUtcNow() => _agora;
    }
}
