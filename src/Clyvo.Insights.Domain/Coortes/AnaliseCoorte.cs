using Clyvo.Insights.Domain.Excecoes;

namespace Clyvo.Insights.Domain.Coortes;

/// <summary>
/// A pergunta que o core não responde: quanto da receita da clínica é
/// atribuível ao produto, e não à inércia dos tutores.
/// </summary>
/// <remarks>
/// <para>
/// A resposta sai da comparação entre o grupo tratado e o grupo de controle.
/// A diferença entre as duas taxas de cumprimento é o efeito do produto; o
/// resto teria acontecido de qualquer jeito.
/// </para>
/// <para>
/// A regra é pura: entram dois <see cref="Coorte"/> e o ticket médio, sai o
/// cálculo. Sem I/O, sem relógio, sem configuração.
/// </para>
/// </remarks>
public sealed class AnaliseCoorte
{
    /// <summary>
    /// Casas decimais dos números publicados. O arredondamento é sempre o
    /// último passo: cada valor deriva do delta cheio, nunca de um número já
    /// arredondado, para o erro não se acumular.
    /// </summary>
    private const int CasasDecimais = 2;

    private AnaliseCoorte(
        Coorte tratado,
        Coorte controle,
        decimal? ticketMedio,
        bool disponivel,
        string? motivoIndisponibilidade,
        decimal deltaPontosPercentuais,
        decimal consultasAtribuiveis,
        decimal receitaRecuperada)
    {
        Tratado = tratado;
        Controle = controle;
        TicketMedio = ticketMedio;
        Disponivel = disponivel;
        MotivoIndisponibilidade = motivoIndisponibilidade;
        DeltaPontosPercentuais = deltaPontosPercentuais;
        ConsultasAtribuiveis = consultasAtribuiveis;
        ReceitaRecuperada = receitaRecuperada;
    }

    /// <summary>Coorte dos pets perseguidos pelo motor.</summary>
    public Coorte Tratado { get; }

    /// <summary>Coorte de controle, o contrafactual.</summary>
    public Coorte Controle { get; }

    /// <summary>
    /// Ticket médio da clínica. Nulo quando a clínica ainda não tem consulta
    /// realizada com valor lançado.
    /// </summary>
    public decimal? TicketMedio { get; }

    /// <summary>
    /// Falso quando falta denominador em algum dos braços. Nesse caso os
    /// números vêm zerados e não devem ser exibidos como resultado.
    /// </summary>
    public bool Disponivel { get; }

    /// <summary>Por que a análise não pôde ser calculada. Nulo quando <see cref="Disponivel"/>.</summary>
    public string? MotivoIndisponibilidade { get; }

    /// <summary>
    /// Diferença entre as taxas de cumprimento, em pontos percentuais.
    /// </summary>
    /// <remarks>
    /// Pode ser negativo, e negativo é preservado: significa que os pets
    /// perseguidos compareceram menos que os deixados de fora. Zerar ou tomar o
    /// módulo esconderia exatamente o resultado que mais importa saber.
    /// </remarks>
    public decimal DeltaPontosPercentuais { get; }

    /// <summary>
    /// Consultas que existem por causa do produto: obrigações resolvidas do
    /// grupo tratado multiplicadas pelo delta.
    /// </summary>
    public decimal ConsultasAtribuiveis { get; }

    /// <summary>
    /// <see cref="ConsultasAtribuiveis"/> ao ticket médio. Zero quando não há
    /// ticket — veja <see cref="ReceitaEstimavel"/>.
    /// </summary>
    public decimal ReceitaRecuperada { get; }

    /// <summary>
    /// Falso quando a receita saiu zerada por falta de ticket médio, e não por
    /// falta de efeito. Sem isso, "R$ 0,00" é ambíguo.
    /// </summary>
    public bool ReceitaEstimavel => Disponivel && TicketMedio is > 0m;

    /// <summary>
    /// Compara os dois braços e calcula o efeito atribuível ao produto.
    /// </summary>
    /// <param name="tratado">Coorte do grupo tratado.</param>
    /// <param name="controle">Coorte do grupo de controle.</param>
    /// <param name="ticketMedio">
    /// Ticket médio da clínica, ou nulo quando não há consulta valorada.
    /// </param>
    /// <exception cref="RegraDeDominioException">
    /// Coorte no braço errado, ou ticket médio negativo.
    /// </exception>
    public static AnaliseCoorte Calcular(Coorte tratado, Coorte controle, decimal? ticketMedio)
    {
        ArgumentNullException.ThrowIfNull(tratado);
        ArgumentNullException.ThrowIfNull(controle);

        if (tratado.Grupo != GrupoCoorte.Tratado)
        {
            throw new RegraDeDominioException(
                $"O primeiro braço precisa ser a coorte tratada, e veio {tratado.Grupo}.");
        }

        if (controle.Grupo != GrupoCoorte.Controle)
        {
            throw new RegraDeDominioException(
                $"O segundo braço precisa ser a coorte de controle, e veio {controle.Grupo}.");
        }

        if (ticketMedio is < 0m)
        {
            throw new RegraDeDominioException(
                $"Ticket médio não pode ser negativo (valor {ticketMedio}).");
        }

        if (controle.Vazia)
        {
            return Indisponivel(tratado, controle, ticketMedio,
                "A coorte de controle não tem obrigação resolvida: sem contrafactual não há delta.");
        }

        if (tratado.Vazia)
        {
            return Indisponivel(tratado, controle, ticketMedio,
                "A coorte tratada não tem obrigação resolvida: não há o que atribuir ao produto.");
        }

        var delta = tratado.TaxaCumprimento - controle.TaxaCumprimento;
        var consultasAtribuiveis = tratado.ObrigacoesResolvidas * delta;
        var receitaRecuperada = consultasAtribuiveis * (ticketMedio ?? 0m);

        return new AnaliseCoorte(
            tratado,
            controle,
            ticketMedio,
            disponivel: true,
            motivoIndisponibilidade: null,
            Arredondar(delta * 100m),
            Arredondar(consultasAtribuiveis),
            Arredondar(receitaRecuperada));
    }

    private static AnaliseCoorte Indisponivel(
        Coorte tratado,
        Coorte controle,
        decimal? ticketMedio,
        string motivo) =>
        new(tratado, controle, ticketMedio,
            disponivel: false,
            motivoIndisponibilidade: motivo,
            deltaPontosPercentuais: 0m,
            consultasAtribuiveis: 0m,
            receitaRecuperada: 0m);

    private static decimal Arredondar(decimal valor) =>
        Math.Round(valor, CasasDecimais, MidpointRounding.AwayFromZero);
}
