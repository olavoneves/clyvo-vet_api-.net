namespace Clyvo.Insights.Api.Hipermidia;

/// <summary>
/// Nomes das rotas que algum link aponta. O gerador resolve a URL pelo nome,
/// então mudar o template de uma rota não quebra link nenhum.
/// </summary>
public static class NomesDeRota
{
    public const string ObterCoorte = nameof(ObterCoorte);

    public const string ObterResumoObrigacoes = nameof(ObterResumoObrigacoes);
    public const string ListarObrigacoes = nameof(ListarObrigacoes);
    public const string ObterObrigacao = nameof(ObterObrigacao);

    public const string ListarMetas = nameof(ListarMetas);
    public const string ObterMeta = nameof(ObterMeta);
    public const string CriarMeta = nameof(CriarMeta);
    public const string AtualizarMeta = nameof(AtualizarMeta);
    public const string RemoverMeta = nameof(RemoverMeta);
}
