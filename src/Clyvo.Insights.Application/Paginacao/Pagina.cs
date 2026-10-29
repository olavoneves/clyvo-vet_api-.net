namespace Clyvo.Insights.Application.Paginacao;

/// <summary>
/// Uma página de resultados e a aritmética de navegação em volta dela.
/// </summary>
/// <remarks>
/// A conta de "existe anterior", "existe próxima" e "qual é a última" mora aqui,
/// e não em quem monta os links: é regra, tem casos de borda — lista vazia,
/// página além da última — e precisa ser a mesma para todo recurso paginado.
/// </remarks>
/// <param name="Itens">Os itens desta página. Vazia quando a página passa da última.</param>
/// <param name="Numero">Número da página, a partir de 1.</param>
/// <param name="Tamanho">Itens por página pedidos — não quantos vieram.</param>
/// <param name="TotalItens">Total de itens que atendem ao filtro, em todas as páginas.</param>
public sealed record Pagina<T>(IReadOnlyList<T> Itens, int Numero, int Tamanho, int TotalItens)
{
    /// <summary>Zero quando não há item nenhum.</summary>
    public int TotalPaginas => (int)(((long)TotalItens + Tamanho - 1) / Tamanho);

    /// <summary>
    /// Destino do link <c>last</c>. Nunca menor que 1: sem itens, a última
    /// página é a primeira, vazia.
    /// </summary>
    public int Ultima => Math.Max(TotalPaginas, 1);

    public bool TemAnterior => Numero > 1;

    /// <summary>
    /// Destino do link <c>prev</c>. Quem pediu uma página além da última volta
    /// para a última, e não para a vizinha vazia — senão o <c>prev</c> levaria
    /// a outra página sem nada.
    /// </summary>
    public int Anterior => Math.Min(Numero - 1, Ultima);

    public bool TemProxima => Numero < TotalPaginas;

    public int Proxima => Numero + 1;

    /// <summary>Mesma página, com os itens projetados.</summary>
    public Pagina<TDestino> Mapear<TDestino>(Func<T, TDestino> projecao) =>
        new(Itens.Select(projecao).ToList(), Numero, Tamanho, TotalItens);
}
