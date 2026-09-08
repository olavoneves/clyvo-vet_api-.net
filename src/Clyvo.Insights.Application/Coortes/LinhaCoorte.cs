using Clyvo.Insights.Domain.Coortes;

namespace Clyvo.Insights.Application.Coortes;

/// <summary>
/// Uma linha de <c>VW_CLV_PAINEL_COORTE</c> como o repositório a devolve.
/// </summary>
/// <remarks>
/// Modelo de leitura, não entidade: a view entrega uma linha por clínica e
/// grupo, e a montagem dos objetos de domínio acontece no caso de uso.
/// </remarks>
/// <param name="Grupo">Braço do experimento.</param>
/// <param name="ObrigacoesResolvidas">
/// <c>QT_OBRIGACOES</c>. A view já filtra por status resolvido, então esta
/// contagem é o denominador — não o total de obrigações da clínica.
/// </param>
/// <param name="ObrigacoesCumpridas"><c>QT_CUMPRIDAS</c>.</param>
/// <param name="PetsDistintos"><c>QT_PETS</c>.</param>
/// <param name="TicketMedio">
/// <c>VL_TICKET_MEDIO</c>. Atributo da clínica, repetido nas duas linhas, e
/// nulo quando ela ainda não tem consulta realizada com valor lançado.
/// </param>
public sealed record LinhaCoorte(
    GrupoCoorte Grupo,
    int ObrigacoesResolvidas,
    int ObrigacoesCumpridas,
    int PetsDistintos,
    decimal? TicketMedio);
