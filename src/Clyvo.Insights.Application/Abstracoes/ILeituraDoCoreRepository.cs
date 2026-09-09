using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Application.Obrigacoes;

namespace Clyvo.Insights.Application.Abstracoes;

/// <summary>
/// Porta de leitura sobre as views que o clyvo-core publica.
/// </summary>
/// <remarks>
/// <para>
/// O core expõe <c>VW_CLV_PAINEL_COORTE</c> e <c>VW_CLV_PAINEL_RECEITA</c>
/// deliberadamente, como contrato de integração. Nenhuma implementação desta
/// porta deve ler tabela interna do core direto, nem replicar em C# a regra de
/// coorte que já está em PL/SQL.
/// </para>
/// <para>
/// O nome diz o que a porta é — leitura do core —, e não o que ela devolvia
/// quando tinha um método só. Uma porta chamada por coorte que também responde
/// pelo funil de obrigações obriga quem lê o código a descobrir pela assinatura
/// que o nome está desatualizado.
/// </para>
/// </remarks>
public interface ILeituraDoCoreRepository
{
    /// <summary>
    /// Linhas de coorte da clínica: uma por grupo presente, no máximo duas.
    /// Clínica sem obrigação resolvida devolve lista vazia.
    /// </summary>
    Task<IReadOnlyList<LinhaCoorte>> ObterCoortesDaClinicaAsync(
        long idClinica,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Funil de obrigações vencidas da clínica, somado sobre os meses e sobre
    /// os dois grupos, vindo de <c>VW_CLV_PAINEL_RECEITA</c>.
    /// </summary>
    /// <remarks>
    /// Esta é a segunda view que o core publica, e a única com a distribuição
    /// por estado. Ler <c>TB_CLV_OBRIGACAO</c> direto daria o número exato e
    /// separaria PREVISTA de CANCELADA, mas ao custo de este serviço passar a
    /// depender de tabela interna do core — que é justamente o acoplamento que
    /// a view existe para evitar.
    /// </remarks>
    Task<LinhaFunilObrigacoes> ObterFunilDaClinicaAsync(
        long idClinica,
        CancellationToken cancellationToken = default);
}
