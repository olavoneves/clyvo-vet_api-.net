namespace Clyvo.Insights.Application.Obrigacoes;

/// <summary>
/// O funil de <c>VW_CLV_PAINEL_RECEITA</c> já somado sobre os meses.
/// </summary>
/// <remarks>
/// A view tem grão mensal e por grupo. Somar as contagens é legítimo; somar
/// <c>PC_COMPARECIMENTO</c> não seria — percentual de percentual não é
/// percentual —, e por isso essa coluna não vem.
/// </remarks>
/// <param name="Total">QT_OBRIGACOES.</param>
/// <param name="AlcancaramNotificada">QT_NOTIFICADAS, cumulativo.</param>
/// <param name="AlcancaramRespondida">QT_RESPONDIDAS, cumulativo.</param>
/// <param name="AlcancaramAgendada">QT_AGENDADAS, cumulativo.</param>
/// <param name="Cumpridas">QT_CUMPRIDAS.</param>
/// <param name="Perdidas">QT_PERDIDAS.</param>
/// <param name="ValorRecuperado">VL_RECUPERADO.</param>
/// <param name="ValorPerdido">VL_PERDIDO.</param>
/// <param name="MesInicio">Primeiro mês com obrigação vencida, ou nulo se não há nenhuma.</param>
/// <param name="MesFim">Último mês com obrigação vencida.</param>
public sealed record LinhaFunilObrigacoes(
    int Total,
    int AlcancaramNotificada,
    int AlcancaramRespondida,
    int AlcancaramAgendada,
    int Cumpridas,
    int Perdidas,
    decimal ValorRecuperado,
    decimal ValorPerdido,
    DateTime? MesInicio,
    DateTime? MesFim);
