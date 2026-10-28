using System.Globalization;
using Clyvo.Insights.Application.Obrigacoes;
using Clyvo.Insights.Application.Paginacao;

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

    public GeradorDeLinks(IResolvedorDeRotas rotas)
    {
        _rotas = rotas;
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

    // ------------------------------------------------------------------
    // Comum
    // ------------------------------------------------------------------

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
