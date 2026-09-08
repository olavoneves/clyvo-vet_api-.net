using Clyvo.Insights.Domain.Excecoes;

namespace Clyvo.Insights.Domain.Obrigacoes;

/// <summary>
/// Distribuição das obrigações vencidas por estado final.
/// </summary>
/// <remarks>
/// <para>
/// A <c>VW_CLV_PAINEL_RECEITA</c> entrega um <b>funil cumulativo</b>, não uma
/// distribuição: <c>qt_notificadas</c> conta tudo que chegou pelo menos até
/// NOTIFICADA, e portanto inclui as respondidas, as agendadas e as cumpridas.
/// Somar as colunas da view daria muito mais que o total, porque a mesma
/// obrigação está contada em várias delas.
/// </para>
/// <para>
/// A conversão de funil para distribuição é subtração de degraus consecutivos,
/// e é ela que mora aqui. O que sai são baldes exclusivos: cada obrigação
/// aparece uma vez só, no estado mais adiantado que alcançou.
/// </para>
/// </remarks>
public sealed class ResumoObrigacoes
{
    private ResumoObrigacoes(
        int total,
        int cumpridas,
        int pararamEmAgendada,
        int pararamEmRespondida,
        int pararamEmNotificada,
        int perdidas,
        int naoTrabalhadas)
    {
        Total = total;
        Cumpridas = cumpridas;
        PararamEmAgendada = pararamEmAgendada;
        PararamEmRespondida = pararamEmRespondida;
        PararamEmNotificada = pararamEmNotificada;
        Perdidas = perdidas;
        NaoTrabalhadas = naoTrabalhadas;
    }

    /// <summary>Obrigações já vencidas da clínica. A view não traz as futuras.</summary>
    public int Total { get; }

    /// <summary>Chegaram ao fim com o tutor comparecendo.</summary>
    public int Cumpridas { get; }

    /// <summary>Viraram agendamento e pararam ali: o tutor marcou e não veio.</summary>
    public int PararamEmAgendada { get; }

    /// <summary>O tutor respondeu e não chegou a marcar.</summary>
    public int PararamEmRespondida { get; }

    /// <summary>Foram notificadas e o tutor nunca respondeu.</summary>
    public int PararamEmNotificada { get; }

    /// <summary>Encerradas como perdidas.</summary>
    public int Perdidas { get; }

    /// <summary>
    /// O resto: venceram sem nunca terem sido notificadas, mais as canceladas
    /// pela clínica.
    /// </summary>
    /// <remarks>
    /// A view não separa PREVISTA de CANCELADA — nenhuma das duas entra nas
    /// colunas do funil, e as duas contam no total. Elas vêm somadas aqui em
    /// vez de serem chutadas para um dos lados: um balde honesto e mal
    /// resolvido é melhor que um número preciso e errado.
    /// </remarks>
    public int NaoTrabalhadas { get; }

    /// <summary>
    /// Converte o funil cumulativo da view na distribuição por estado.
    /// </summary>
    /// <param name="total">QT_OBRIGACOES somado.</param>
    /// <param name="alcancaramNotificada">QT_NOTIFICADAS somado — cumulativo.</param>
    /// <param name="alcancaramRespondida">QT_RESPONDIDAS somado — cumulativo.</param>
    /// <param name="alcancaramAgendada">QT_AGENDADAS somado — cumulativo.</param>
    /// <param name="cumpridas">QT_CUMPRIDAS somado.</param>
    /// <param name="perdidas">QT_PERDIDAS somado.</param>
    /// <exception cref="RegraDeDominioException">
    /// Contagem negativa, ou degraus fora de ordem — funil em que um degrau
    /// mais avançado tem mais obrigações que o anterior não é funil.
    /// </exception>
    public static ResumoObrigacoes APartirDoFunil(
        int total,
        int alcancaramNotificada,
        int alcancaramRespondida,
        int alcancaramAgendada,
        int cumpridas,
        int perdidas)
    {
        GarantirNaoNegativo(total, nameof(total));
        GarantirNaoNegativo(alcancaramNotificada, nameof(alcancaramNotificada));
        GarantirNaoNegativo(alcancaramRespondida, nameof(alcancaramRespondida));
        GarantirNaoNegativo(alcancaramAgendada, nameof(alcancaramAgendada));
        GarantirNaoNegativo(cumpridas, nameof(cumpridas));
        GarantirNaoNegativo(perdidas, nameof(perdidas));

        GarantirDegrau(alcancaramNotificada, alcancaramRespondida, "notificada", "respondida");
        GarantirDegrau(alcancaramRespondida, alcancaramAgendada, "respondida", "agendada");
        GarantirDegrau(alcancaramAgendada, cumpridas, "agendada", "cumprida");

        if (alcancaramNotificada + perdidas > total)
        {
            throw new RegraDeDominioException(
                $"O funil soma {alcancaramNotificada + perdidas} obrigações entre notificadas e " +
                $"perdidas, mas o total é {total}. Os dois conjuntos são disjuntos e não podem " +
                "passar do total.");
        }

        return new ResumoObrigacoes(
            total,
            cumpridas,
            pararamEmAgendada: alcancaramAgendada - cumpridas,
            pararamEmRespondida: alcancaramRespondida - alcancaramAgendada,
            pararamEmNotificada: alcancaramNotificada - alcancaramRespondida,
            perdidas,
            naoTrabalhadas: total - alcancaramNotificada - perdidas);
    }

    private static void GarantirNaoNegativo(int valor, string nome)
    {
        if (valor < 0)
        {
            throw new RegraDeDominioException($"'{nome}' não pode ser negativo (valor {valor}).");
        }
    }

    private static void GarantirDegrau(int anterior, int seguinte, string nomeAnterior, string nomeSeguinte)
    {
        if (seguinte > anterior)
        {
            throw new RegraDeDominioException(
                $"O funil tem {seguinte} obrigações em '{nomeSeguinte}' e apenas {anterior} em " +
                $"'{nomeAnterior}'. Todo degrau é subconjunto do anterior, então isso indica " +
                "leitura de colunas trocadas.");
        }
    }
}
