namespace Clyvo.Insights.Application.Excecoes;

/// <summary>
/// A operação bate com o estado atual — por exemplo, criar a segunda meta para
/// o mesmo indicador da mesma clínica.
/// </summary>
public sealed class ConflitoDeRecursoException : Exception
{
    public ConflitoDeRecursoException(string mensagem) : base(mensagem)
    {
    }
}
