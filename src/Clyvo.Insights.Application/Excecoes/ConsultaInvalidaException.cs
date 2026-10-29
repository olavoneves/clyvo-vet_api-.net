namespace Clyvo.Insights.Application.Excecoes;

/// <summary>
/// Parâmetros de consulta fora do que o recurso aceita — página zero, tamanho
/// acima do máximo, campo de ordenação fora da lista.
/// </summary>
/// <remarks>
/// Carrega todos os erros de uma vez, por parâmetro, em vez de parar no
/// primeiro: quem erra dois parâmetros descobre os dois na mesma resposta. A
/// API traduz para 400 com os erros no <c>ProblemDetails</c>.
/// </remarks>
public sealed class ConsultaInvalidaException : Exception
{
    public ConsultaInvalidaException(IReadOnlyDictionary<string, string[]> erros)
        : base("Parâmetros de consulta inválidos: " + string.Join(" ", erros.SelectMany(e => e.Value)))
    {
        Erros = erros;
    }

    /// <summary>Mensagens por nome de parâmetro, como o cliente o escreveu na query.</summary>
    public IReadOnlyDictionary<string, string[]> Erros { get; }
}
