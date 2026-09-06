namespace Clyvo.Insights.Application.Excecoes;

/// <summary>
/// Recurso inexistente, ou existente em outra clínica.
/// </summary>
/// <remarks>
/// As duas situações devolvem a mesma coisa de propósito: distinguir "não
/// existe" de "existe mas é de outro tenant" confirmaria ao chamador que aquele
/// id existe em algum lugar, e isso já é informação vazando entre clínicas.
/// </remarks>
public sealed class RecursoNaoEncontradoException : Exception
{
    public RecursoNaoEncontradoException(string mensagem) : base(mensagem)
    {
    }
}
