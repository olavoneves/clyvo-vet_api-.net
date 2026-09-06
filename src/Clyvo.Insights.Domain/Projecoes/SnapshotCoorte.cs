using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Domain.Excecoes;

namespace Clyvo.Insights.Domain.Projecoes;

/// <summary>
/// Uma apuração da análise de coorte congelada num dia.
/// </summary>
/// <remarks>
/// <para>
/// A série de snapshots é o que permite responder "quando o delta caiu", que a
/// view não responde: ela só sabe dizer como está agora. Sem histórico, uma
/// queda de comparecimento só é percebida quando alguém lembra do número
/// anterior.
/// </para>
/// <para>
/// O grão é diário de propósito. Consultar dez vezes no mesmo dia não deve
/// produzir dez pontos na série — a análise varia com o desfecho das
/// obrigações, não com a frequência de quem olha o painel.
/// </para>
/// </remarks>
public sealed class SnapshotCoorte
{
    private SnapshotCoorte(
        long idClinica,
        DateOnly dataReferencia,
        decimal deltaPontosPercentuais,
        decimal consultasAtribuiveis,
        decimal receitaRecuperada,
        decimal? ticketMedio,
        int obrigacoesResolvidasTratado,
        int obrigacoesResolvidasControle)
    {
        IdClinica = idClinica;
        DataReferencia = dataReferencia;
        DeltaPontosPercentuais = deltaPontosPercentuais;
        ConsultasAtribuiveis = consultasAtribuiveis;
        ReceitaRecuperada = receitaRecuperada;
        TicketMedio = ticketMedio;
        ObrigacoesResolvidasTratado = obrigacoesResolvidasTratado;
        ObrigacoesResolvidasControle = obrigacoesResolvidasControle;
    }

    /// <summary>Chave técnica de <c>INS_SNAPSHOT_COORTE</c>.</summary>
    public long Id { get; private set; }

    public long IdClinica { get; private set; }

    /// <summary>Dia da apuração. É a chave da série, junto com a clínica.</summary>
    public DateOnly DataReferencia { get; private set; }

    public decimal DeltaPontosPercentuais { get; private set; }

    public decimal ConsultasAtribuiveis { get; private set; }

    public decimal ReceitaRecuperada { get; private set; }

    public decimal? TicketMedio { get; private set; }

    public int ObrigacoesResolvidasTratado { get; private set; }

    public int ObrigacoesResolvidasControle { get; private set; }

    /// <summary>
    /// Congela a análise no dia.
    /// </summary>
    /// <exception cref="RegraDeDominioException">
    /// Análise indisponível — sem contrafactual não há o que congelar, e gravar
    /// os zeros deixaria na série um ponto indistinguível de "delta zero".
    /// </exception>
    public static SnapshotCoorte De(long idClinica, AnaliseCoorte analise, DateOnly dataReferencia)
    {
        ArgumentNullException.ThrowIfNull(analise);

        GarantirApuravel(analise);

        return new SnapshotCoorte(
            idClinica,
            dataReferencia,
            analise.DeltaPontosPercentuais,
            analise.ConsultasAtribuiveis,
            analise.ReceitaRecuperada,
            analise.TicketMedio,
            analise.Tratado.ObrigacoesResolvidas,
            analise.Controle.ObrigacoesResolvidas);
    }

    /// <summary>
    /// Reescreve o ponto do dia com uma apuração mais recente.
    /// </summary>
    /// <remarks>
    /// A segunda consulta do mesmo dia não acrescenta ponto à série: ela
    /// corrige o que já estava lá. Obrigação que mudou de estado entre as duas
    /// consultas fica refletida, e a série continua com um ponto por dia.
    /// </remarks>
    /// <exception cref="RegraDeDominioException">Análise indisponível.</exception>
    public void Reapurar(AnaliseCoorte analise)
    {
        ArgumentNullException.ThrowIfNull(analise);
        GarantirApuravel(analise);

        DeltaPontosPercentuais = analise.DeltaPontosPercentuais;
        ConsultasAtribuiveis = analise.ConsultasAtribuiveis;
        ReceitaRecuperada = analise.ReceitaRecuperada;
        TicketMedio = analise.TicketMedio;
        ObrigacoesResolvidasTratado = analise.Tratado.ObrigacoesResolvidas;
        ObrigacoesResolvidasControle = analise.Controle.ObrigacoesResolvidas;
    }

    private static void GarantirApuravel(AnaliseCoorte analise)
    {
        if (!analise.Disponivel)
        {
            throw new RegraDeDominioException(
                "Análise indisponível não vira snapshot: os números zerados entrariam na série " +
                "como se fossem uma apuração de delta zero.");
        }
    }
}
