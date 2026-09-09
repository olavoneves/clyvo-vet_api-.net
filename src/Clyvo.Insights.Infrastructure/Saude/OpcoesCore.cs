namespace Clyvo.Insights.Infrastructure.Saude;

/// <summary>
/// Onde encontrar o clyvo-core.
/// </summary>
/// <remarks>
/// O insights não chama o core para servir requisição — ele valida localmente o
/// token que o core emitiu. Esta configuração existe só para a sonda de
/// disponibilidade: saber que o emissor está de pé é informação de operação, e
/// não caminho de dados.
/// </remarks>
public sealed class OpcoesCore
{
    public const string Secao = "Core";

    /// <summary>Base do clyvo-core. Vazio desliga a sonda.</summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Caminho da sonda. O core publica <c>/actuator/health</c> sem
    /// autenticação justamente para isso.
    /// </summary>
    public string CaminhoDeSaude { get; init; } = "/actuator/health";

    /// <summary>
    /// Curto de propósito: a sonda não pode segurar o <c>/health/ready</c> de
    /// quem a chama. Core lento é core indisponível para efeito de operação.
    /// </summary>
    public int TimeoutSegundos { get; init; } = 2;
}
