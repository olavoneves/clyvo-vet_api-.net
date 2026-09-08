namespace Clyvo.Insights.Application.Obrigacoes;

/// <summary>
/// Distribuição das obrigações vencidas da clínica por estado final.
/// </summary>
/// <param name="IdClinica">Clínica do token.</param>
/// <param name="Total">Obrigações já vencidas. As futuras não entram — não tiveram chance ainda.</param>
/// <param name="Cumpridas">Terminaram com o tutor comparecendo.</param>
/// <param name="PararamEmAgendada">Viraram agendamento e o tutor não veio.</param>
/// <param name="PararamEmRespondida">O tutor respondeu e não chegou a marcar.</param>
/// <param name="PararamEmNotificada">Foram notificadas e o tutor nunca respondeu.</param>
/// <param name="Perdidas">Encerradas como perdidas.</param>
/// <param name="NaoTrabalhadas">
/// Venceram sem nunca terem sido notificadas, mais as canceladas pela clínica.
/// A view não separa as duas.
/// </param>
/// <param name="ValorRecuperado">Soma do valor realizado.</param>
/// <param name="ValorPerdido">Valor estimado das obrigações perdidas.</param>
/// <param name="MesInicio">Primeiro mês da janela coberta.</param>
/// <param name="MesFim">Último mês da janela coberta.</param>
public sealed record ResumoObrigacoesDto(
    long IdClinica,
    int Total,
    int Cumpridas,
    int PararamEmAgendada,
    int PararamEmRespondida,
    int PararamEmNotificada,
    int Perdidas,
    int NaoTrabalhadas,
    decimal ValorRecuperado,
    decimal ValorPerdido,
    DateTime? MesInicio,
    DateTime? MesFim);
