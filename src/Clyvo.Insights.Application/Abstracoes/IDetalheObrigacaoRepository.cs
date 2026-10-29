using Clyvo.Insights.Application.Obrigacoes;
using Clyvo.Insights.Application.Paginacao;

namespace Clyvo.Insights.Application.Abstracoes;

/// <summary>
/// Porta de leitura sobre <c>VW_CLV_OBRIGACAO_DETALHE</c>, a terceira view que
/// o core publica — uma linha por obrigação.
/// </summary>
/// <remarks>
/// <para>
/// Porta separada de <see cref="ILeituraDoCoreRepository"/> de propósito: os
/// casos de uso de coorte e de resumo leem agregados e não têm por que depender
/// de paginação; a listagem não tem por que depender de coorte.
/// </para>
/// <para>
/// Como nas metas, todo método recebe a clínica e filtra por ela. Não existe
/// busca por id sozinho — a que existisse seria usada uma vez sem o filtro.
/// </para>
/// </remarks>
public interface IDetalheObrigacaoRepository
{
    /// <summary>
    /// Uma página das obrigações da clínica que atendem aos critérios.
    /// </summary>
    /// <remarks>
    /// Filtro, ordenação, corte da página e contagem acontecem no banco. O total
    /// sai da mesma consulta filtrada que produz os itens, para os dois números
    /// nunca descreverem conjuntos diferentes.
    /// </remarks>
    Task<Pagina<LinhaObrigacao>> ListarDaClinicaAsync(
        long idClinica,
        CriteriosListagemObrigacoes criterios,
        CancellationToken cancellationToken = default);

    /// <summary>Uma obrigação por id <b>e</b> clínica. Nula se não existir ou for de outra clínica.</summary>
    Task<LinhaObrigacao?> ObterDaClinicaAsync(
        long idClinica,
        long idObrigacao,
        CancellationToken cancellationToken = default);
}
