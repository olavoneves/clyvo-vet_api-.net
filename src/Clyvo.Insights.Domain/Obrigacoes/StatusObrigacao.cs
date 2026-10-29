namespace Clyvo.Insights.Domain.Obrigacoes;

/// <summary>
/// Estado de uma obrigação no ciclo do motor, na ordem em que ele a conduz.
/// </summary>
/// <remarks>
/// <para>
/// Os valores espelham o <c>CHK_OBRIGACAO_STATUS</c> do core. O insights não
/// muda estado de obrigação nenhuma — ele só lê —, mas precisa conhecer o
/// vocabulário para recusar um filtro com erro de digitação em vez de devolver
/// uma lista vazia que parece resposta.
/// </para>
/// <para>
/// A ordem numérica é a do funil, e é ela que a listagem usa ao ordenar por
/// status: ordem alfabética poria AGENDADA antes de PREVISTA, que é o contrário
/// do que acontece com a obrigação.
/// </para>
/// </remarks>
public enum StatusObrigacao
{
    /// <summary>Gerada pelo motor, ainda não notificada.</summary>
    Prevista = 1,

    /// <summary>O tutor recebeu o lembrete.</summary>
    Notificada = 2,

    /// <summary>O tutor respondeu ao lembrete.</summary>
    Respondida = 3,

    /// <summary>Virou agendamento.</summary>
    Agendada = 4,

    /// <summary>O tutor compareceu.</summary>
    Cumprida = 5,

    /// <summary>A janela fechou sem comparecimento.</summary>
    Perdida = 6,

    /// <summary>Cancelada pela clínica.</summary>
    Cancelada = 7
}

/// <summary>
/// Tradução entre o enum e o código que o core grava em <c>DS_STATUS</c>.
/// </summary>
public static class CodigoDeStatus
{
    /// <summary>O código como o core grava: o nome em maiúsculas.</summary>
    public static string Codigo(this StatusObrigacao status) => status.ToString().ToUpperInvariant();

    /// <summary>
    /// Interpreta um código de status, sem diferenciar maiúsculas. Número não é
    /// aceito: <c>Enum.TryParse</c> aceitaria "3" como Respondida, e um filtro
    /// por número de enum é contrato que ninguém combinou.
    /// </summary>
    public static bool TentarInterpretar(string? codigo, out StatusObrigacao status)
    {
        status = default;

        if (string.IsNullOrWhiteSpace(codigo))
        {
            return false;
        }

        var nome = Enum.GetNames<StatusObrigacao>()
            .FirstOrDefault(n => string.Equals(n, codigo.Trim(), StringComparison.OrdinalIgnoreCase));

        if (nome is null)
        {
            return false;
        }

        status = Enum.Parse<StatusObrigacao>(nome);

        return true;
    }
}
