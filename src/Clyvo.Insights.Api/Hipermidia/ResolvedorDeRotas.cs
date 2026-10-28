namespace Clyvo.Insights.Api.Hipermidia;

/// <summary>Transforma nome de rota e valores em URL absoluta.</summary>
/// <remarks>
/// Existe para o <see cref="GeradorDeLinks"/> poder ser testado sem servidor:
/// a decisão de quais links existem e com quais parâmetros é dele, e a de como
/// uma rota vira URL é do ASP.NET.
/// </remarks>
public interface IResolvedorDeRotas
{
    /// <param name="rota">Nome da rota, de <see cref="NomesDeRota"/>.</param>
    /// <param name="valores">
    /// Valores de rota. Os que não fazem parte do template viram query string.
    /// </param>
    string Url(string rota, IReadOnlyDictionary<string, string>? valores = null);
}

/// <summary>
/// Resolve pelo <c>LinkGenerator</c> do ASP.NET Core, com o esquema, o host e o
/// path base da requisição em curso.
/// </summary>
internal sealed class ResolvedorDeRotasHttp : IResolvedorDeRotas
{
    private readonly LinkGenerator _gerador;
    private readonly IHttpContextAccessor _acessor;

    public ResolvedorDeRotasHttp(LinkGenerator gerador, IHttpContextAccessor acessor)
    {
        _gerador = gerador;
        _acessor = acessor;
    }

    public string Url(string rota, IReadOnlyDictionary<string, string>? valores = null)
    {
        var contexto = _acessor.HttpContext
            ?? throw new InvalidOperationException("Link montado fora de uma requisição HTTP.");

        var valoresDeRota = new RouteValueDictionary();

        foreach (var (chave, valor) in valores ?? new Dictionary<string, string>())
        {
            valoresDeRota[chave] = valor;
        }

        return _gerador.GetUriByName(contexto, rota, valoresDeRota)
            ?? throw new InvalidOperationException(
                $"A rota '{rota}' não existe ou não aceita os valores informados.");
    }
}
