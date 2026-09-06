namespace Clyvo.Insights.Domain.Excecoes;

/// <summary>
/// Violação de invariante do domínio. Sinaliza erro de quem chamou, não estado
/// de negócio possível: a API traduz para 400, nunca para 500.
/// </summary>
public sealed class RegraDeDominioException : Exception
{
    public RegraDeDominioException(string mensagem) : base(mensagem)
    {
    }
}
