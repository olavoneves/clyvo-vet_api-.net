using Clyvo.Insights.Domain.Excecoes;

namespace Clyvo.Insights.Domain.Metas;

/// <summary>
/// O piso que a clínica define para um indicador — por exemplo, taxa mínima de
/// cumprimento de 55%. O insights sinaliza quando o valor apurado fica abaixo.
/// </summary>
/// <remarks>
/// É o único dado do qual este serviço é dono, e mora em <c>INS_META_INDICADOR</c>.
/// Deliberadamente enxuto: um indicador e um limiar. Meta não guarda histórico,
/// não dispara notificação e não tem estado — quem quiser isso está pedindo
/// outro agregado.
/// </remarks>
public sealed class MetaIndicador
{
    private const decimal LimiarMinimo = 0m;
    private const decimal LimiarMaximo = 100m;

    private MetaIndicador(
        long id,
        long idClinica,
        IndicadorMonitorado indicador,
        decimal limiarPercentual,
        DateTime dataCriacao,
        DateTime dataAtualizacao)
    {
        Id = id;
        IdClinica = idClinica;
        Indicador = indicador;
        LimiarPercentual = limiarPercentual;
        DataCriacao = dataCriacao;
        DataAtualizacao = dataAtualizacao;
    }

    public long Id { get; private set; }

    /// <summary>Clínica dona da meta. Vem do token, nunca do cliente.</summary>
    public long IdClinica { get; private set; }

    public IndicadorMonitorado Indicador { get; private set; }

    /// <summary>Piso aceitável, de 0 a 100.</summary>
    public decimal LimiarPercentual { get; private set; }

    public DateTime DataCriacao { get; private set; }

    public DateTime DataAtualizacao { get; private set; }

    /// <summary>
    /// Cria a meta validando as invariantes.
    /// </summary>
    /// <param name="agora">
    /// Relógio de quem chamou. O domínio não lê a hora do sistema — regra que
    /// consulta o relógio por dentro não dá para testar sem esperar o tempo
    /// passar.
    /// </param>
    /// <exception cref="RegraDeDominioException">
    /// Clínica inválida, indicador desconhecido ou limiar fora de 0 a 100.
    /// </exception>
    public static MetaIndicador Criar(
        long idClinica,
        IndicadorMonitorado indicador,
        decimal limiarPercentual,
        DateTime agora)
    {
        if (idClinica <= 0)
        {
            throw new RegraDeDominioException($"Clínica inválida para a meta (valor {idClinica}).");
        }

        GarantirIndicadorConhecido(indicador);
        GarantirLimiarNaFaixa(limiarPercentual);

        return new MetaIndicador(id: 0, idClinica, indicador, limiarPercentual, agora, agora);
    }

    /// <summary>Move o piso. É a única coisa que a meta permite alterar.</summary>
    /// <exception cref="RegraDeDominioException">Limiar fora de 0 a 100.</exception>
    public void AlterarLimiar(decimal novoLimiarPercentual, DateTime agora)
    {
        GarantirLimiarNaFaixa(novoLimiarPercentual);

        LimiarPercentual = novoLimiarPercentual;
        DataAtualizacao = agora;
    }

    /// <summary>
    /// Se o valor apurado ficou abaixo do piso. Estritamente abaixo: apurar
    /// exatamente o limiar é atingir a meta, não falhar nela.
    /// </summary>
    public bool EstaAbaixoDoLimiar(decimal valorApurado) => valorApurado < LimiarPercentual;

    private static void GarantirIndicadorConhecido(IndicadorMonitorado indicador)
    {
        if (!Enum.IsDefined(indicador))
        {
            throw new RegraDeDominioException(
                $"Indicador desconhecido (valor {(int)indicador}). " +
                $"Esperado um de: {string.Join(", ", Enum.GetNames<IndicadorMonitorado>())}.");
        }
    }

    private static void GarantirLimiarNaFaixa(decimal limiarPercentual)
    {
        if (limiarPercentual is < LimiarMinimo or > LimiarMaximo)
        {
            throw new RegraDeDominioException(
                $"Limiar precisa estar entre {LimiarMinimo} e {LimiarMaximo} (valor {limiarPercentual}).");
        }
    }
}
