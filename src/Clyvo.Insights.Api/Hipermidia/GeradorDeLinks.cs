using System.Globalization;
using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Application.Obrigacoes;
using Clyvo.Insights.Application.Paginacao;
using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Domain.Obrigacoes;

namespace Clyvo.Insights.Api.Hipermidia;

/// <summary>
/// O único lugar que decide quais links cada recurso carrega.
/// </summary>
/// <remarks>
/// <para>
/// Os controllers pedem "a obrigação com os links dela" e devolvem o que
/// recebem. Se cada um montasse os próprios links, o formato de
/// <c>_links</c> e a regra de quando <c>prev</c> aparece seriam reescritos — e
/// divergiriam — em cada controller.
/// </para>
/// <para>
/// A URL sai do nome da rota, nunca de string concatenada: mudar o template de
/// uma rota não deixa link quebrado para trás.
/// </para>
/// </remarks>
public sealed class GeradorDeLinks
{
    private readonly IResolvedorDeRotas _rotas;
    private readonly TimeProvider _relogio;

    public GeradorDeLinks(IResolvedorDeRotas rotas, TimeProvider relogio)
    {
        _rotas = rotas;
        _relogio = relogio;
    }

    // ------------------------------------------------------------------
    // Obrigações
    // ------------------------------------------------------------------

    /// <summary>Uma obrigação, com o <c>self</c> apontando para o detalhe.</summary>
    public Recurso<ObrigacaoDto> ParaObrigacao(ObrigacaoDto obrigacao) =>
        new(obrigacao, new Dictionary<string, Link>
        {
            ["self"] = Link(NomesDeRota.ObterObrigacao, Id(obrigacao.Id))
        });

    /// <summary>
    /// A página da listagem, com cada item e a navegação. Todos os links de
    /// página repetem os filtros e a ordenação dos critérios, mudando só o
    /// número da página.
    /// </summary>
    public PaginaDeRecursos<ObrigacaoDto> ParaListagemDeObrigacoes(ListagemObrigacoesDto listagem)
    {
        var pagina = listagem.Pagina;

        return new PaginaDeRecursos<ObrigacaoDto>(
            pagina.Itens.Select(ParaObrigacao).ToList(),
            pagina.Numero,
            pagina.Tamanho,
            pagina.TotalItens,
            pagina.TotalPaginas,
            Navegacao(pagina, numero =>
                Link(NomesDeRota.ListarObrigacoes, listagem.Criterios.NaPagina(numero).ParaParametrosDeConsulta())));
    }

    /// <summary>
    /// O resumo por estado, com um link por balde para a listagem filtrada
    /// pelo status correspondente.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A relação de cada link tem o nome do campo do resumo, para o cliente
    /// achar o link do número que está mostrando sem tabela de tradução.
    /// <c>naoTrabalhadas</c> soma dois status, e por isso vira dois links.
    /// </para>
    /// <para>
    /// Os links levam <c>ate</c> no dia anterior ao de hoje: o resumo só conta
    /// obrigação vencida (<c>DT_PREVISTA &lt; TRUNC(SYSDATE)</c> na view), e a
    /// listagem sem esse corte misturaria as futuras. O balde é derivado do
    /// funil cumulativo, então a contagem da listagem é o mesmo recorte, não
    /// necessariamente o mesmo número.
    /// </para>
    /// </remarks>
    public Recurso<ResumoObrigacoesDto> ParaResumoDeObrigacoes(ResumoObrigacoesDto resumo)
    {
        var ontem = DateOnly.FromDateTime(_relogio.GetUtcNow().UtcDateTime).AddDays(-1)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var links = new Dictionary<string, Link>
        {
            ["self"] = Link(NomesDeRota.ObterResumoObrigacoes)
        };

        foreach (var (relacao, status) in BaldesDoResumo)
        {
            links[relacao] = Link(NomesDeRota.ListarObrigacoes, new Dictionary<string, string>
            {
                ["status"] = status.Codigo(),
                ["ate"] = ontem
            });
        }

        return new Recurso<ResumoObrigacoesDto>(resumo, links);
    }

    // ------------------------------------------------------------------
    // Coorte
    // ------------------------------------------------------------------

    /// <summary>
    /// A análise, com um link para as obrigações de cada braço — de onde saem
    /// os números de tratado e de controle.
    /// </summary>
    public Recurso<AnaliseCoorteDto> ParaAnaliseDeCoorte(AnaliseCoorteDto analise) =>
        new(analise, new Dictionary<string, Link>
        {
            ["self"] = Link(NomesDeRota.ObterCoorte),
            ["obrigacoesTratamento"] = ListagemDoGrupo(GrupoCoorte.Tratado),
            ["obrigacoesControle"] = ListagemDoGrupo(GrupoCoorte.Controle)
        });

    // ------------------------------------------------------------------
    // Metas
    // ------------------------------------------------------------------

    /// <summary>Uma meta, com o que se pode fazer com ela e o método de cada ação.</summary>
    public Recurso<MetaIndicadorDto> ParaMeta(MetaIndicadorDto meta)
    {
        var id = Id(meta.Id);

        return new Recurso<MetaIndicadorDto>(meta, new Dictionary<string, Link>
        {
            ["self"] = Link(NomesDeRota.ObterMeta, id),
            ["atualizar"] = Link(NomesDeRota.AtualizarMeta, id, HttpMethods.Put),
            ["remover"] = Link(NomesDeRota.RemoverMeta, id, HttpMethods.Delete)
        });
    }

    /// <summary>As metas da clínica, cada uma com os próprios links, e a ação de criar.</summary>
    public ColecaoDeRecursos<MetaIndicadorDto> ParaMetas(IReadOnlyList<MetaIndicadorDto> metas) =>
        new(metas.Select(ParaMeta).ToList(), new Dictionary<string, Link>
        {
            ["self"] = Link(NomesDeRota.ListarMetas),
            ["criar"] = Link(NomesDeRota.CriarMeta, metodo: HttpMethods.Post)
        });

    // ------------------------------------------------------------------
    // Comum
    // ------------------------------------------------------------------

    private static readonly (string Relacao, StatusObrigacao Status)[] BaldesDoResumo =
    {
        ("cumpridas", StatusObrigacao.Cumprida),
        ("pararamEmAgendada", StatusObrigacao.Agendada),
        ("pararamEmRespondida", StatusObrigacao.Respondida),
        ("pararamEmNotificada", StatusObrigacao.Notificada),
        ("perdidas", StatusObrigacao.Perdida),
        ("naoTrabalhadasPrevistas", StatusObrigacao.Prevista),
        ("naoTrabalhadasCanceladas", StatusObrigacao.Cancelada)
    };

    private Link ListagemDoGrupo(GrupoCoorte grupo) =>
        Link(NomesDeRota.ListarObrigacoes, new Dictionary<string, string>
        {
            ["grupo"] = CriteriosListagemObrigacoes.TextoDoGrupo(grupo)
        });

    /// <summary>
    /// <c>self</c>, <c>first</c>, <c>prev</c>, <c>next</c> e <c>last</c> de
    /// qualquer recurso paginado. Quais existem e para que página apontam é
    /// decidido por <see cref="Pagina{T}"/>; aqui só se monta.
    /// </summary>
    private static IReadOnlyDictionary<string, Link> Navegacao<T>(Pagina<T> pagina, Func<int, Link> linkDaPagina)
    {
        var links = new Dictionary<string, Link>
        {
            ["self"] = linkDaPagina(pagina.Numero),
            ["first"] = linkDaPagina(1)
        };

        if (pagina.TemAnterior)
        {
            links["prev"] = linkDaPagina(pagina.Anterior);
        }

        if (pagina.TemProxima)
        {
            links["next"] = linkDaPagina(pagina.Proxima);
        }

        links["last"] = linkDaPagina(pagina.Ultima);

        return links;
    }

    private Link Link(string rota, IReadOnlyDictionary<string, string>? valores = null, string? metodo = null) =>
        new(_rotas.Url(rota, valores), metodo);

    private static IReadOnlyDictionary<string, string> Id(long id) =>
        new Dictionary<string, string> { ["id"] = id.ToString(CultureInfo.InvariantCulture) };
}
