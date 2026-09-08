namespace Clyvo.Insights.Api.Autenticacao;

/// <summary>
/// Chave simétrica compartilhada com o clyvo-core.
/// </summary>
/// <remarks>
/// O insights não emite token: ele valida o que o core emitiu. A chave é a
/// mesma dos dois lados, lida de configuração, e nunca é versionada — em
/// desenvolvimento vem por variável de ambiente <c>Jwt__Secret</c>.
/// </remarks>
public sealed class OpcoesJwt
{
    public const string Secao = "Jwt";

    /// <summary>Segredo HMAC. O core usa HS256 sobre os bytes UTF-8 desta string.</summary>
    public string Secret { get; init; } = string.Empty;
}
