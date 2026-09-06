using Clyvo.Insights.Application.Coortes;

namespace Clyvo.Insights.Application.Abstracoes;

/// <summary>
/// Porta de leitura sobre as views que o clyvo-core publica.
/// </summary>
/// <remarks>
/// O core expõe <c>VW_CLV_PAINEL_COORTE</c> deliberadamente, como contrato de
/// integração. Nenhuma implementação desta porta deve ler tabela interna do
/// core direto, nem replicar em C# a regra de coorte que já está em PL/SQL.
/// </remarks>
public interface ICoorteReadRepository
{
    /// <summary>
    /// Linhas de coorte da clínica: uma por grupo presente, no máximo duas.
    /// Clínica sem obrigação resolvida devolve lista vazia.
    /// </summary>
    Task<IReadOnlyList<LinhaCoorte>> ObterCoortesDaClinicaAsync(
        long idClinica,
        CancellationToken cancellationToken = default);
}
