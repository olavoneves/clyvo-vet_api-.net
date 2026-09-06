using Clyvo.Insights.Domain.Excecoes;

namespace Clyvo.Insights.Domain.Coortes;

/// <summary>
/// Um braço do experimento em números: quantas obrigações já foram resolvidas e
/// quantas dessas foram cumpridas.
/// </summary>
/// <remarks>
/// <para>
/// <b>O denominador é só o que já foi resolvido</b> — obrigação CUMPRIDA ou
/// PERDIDA. Obrigação que ainda não venceu não é falta: ela nem teve a chance.
/// Contá-la afundaria as duas taxas e, pior, faria o número andar sozinho com o
/// calendário — a mesma coorte "pioraria" a cada mês em que nada acontecesse.
/// </para>
/// <para>
/// A decisão já está tomada do lado Oracle, na <c>VW_CLV_PAINEL_COORTE</c>, que
/// filtra por <c>ds_status IN ('CUMPRIDA','PERDIDA')</c>. Aqui ela é preservada
/// como invariante: cumpridas nunca podem passar de resolvidas, e uma coorte
/// que chegue com total não resolvido é erro de quem chamou.
/// </para>
/// <para>
/// A taxa de cumprimento é derivada, nunca recebida pronta — receber o
/// percentual já calculado permitiria que ele divergisse do numerador e do
/// denominador que o acompanham.
/// </para>
/// </remarks>
public sealed class Coorte
{
    private Coorte(GrupoCoorte grupo, int obrigacoesResolvidas, int obrigacoesCumpridas, int petsDistintos)
    {
        Grupo = grupo;
        ObrigacoesResolvidas = obrigacoesResolvidas;
        ObrigacoesCumpridas = obrigacoesCumpridas;
        PetsDistintos = petsDistintos;
    }

    /// <summary>Braço do experimento.</summary>
    public GrupoCoorte Grupo { get; }

    /// <summary>Obrigações que chegaram ao fim — cumpridas mais perdidas. É o denominador.</summary>
    public int ObrigacoesResolvidas { get; }

    /// <summary>Obrigações resolvidas com o tutor comparecendo. É o numerador.</summary>
    public int ObrigacoesCumpridas { get; }

    /// <summary>
    /// Tamanho da coorte em pets, e não em obrigações. É ele que diz se o delta
    /// se sustenta: dez obrigações de um pet só não são uma amostra.
    /// </summary>
    public int PetsDistintos { get; }

    /// <summary>Nenhuma obrigação resolvida ainda. Sem denominador, não há taxa.</summary>
    public bool Vazia => ObrigacoesResolvidas == 0;

    /// <summary>
    /// Fração de obrigações resolvidas que foram cumpridas, entre 0 e 1.
    /// Coorte vazia devolve zero em vez de dividir por zero — quem precisa
    /// distinguir "zero por cento" de "sem dados" olha <see cref="Vazia"/>.
    /// </summary>
    public decimal TaxaCumprimento => Vazia
        ? 0m
        : (decimal)ObrigacoesCumpridas / ObrigacoesResolvidas;

    /// <summary>
    /// Monta a coorte validando as invariantes.
    /// </summary>
    /// <exception cref="RegraDeDominioException">
    /// Contagem negativa, ou mais cumpridas do que resolvidas.
    /// </exception>
    public static Coorte Criar(
        GrupoCoorte grupo,
        int obrigacoesResolvidas,
        int obrigacoesCumpridas,
        int petsDistintos)
    {
        if (obrigacoesResolvidas < 0)
        {
            throw new RegraDeDominioException(
                $"Obrigações resolvidas não pode ser negativo (grupo {grupo}, valor {obrigacoesResolvidas}).");
        }

        if (obrigacoesCumpridas < 0)
        {
            throw new RegraDeDominioException(
                $"Obrigações cumpridas não pode ser negativo (grupo {grupo}, valor {obrigacoesCumpridas}).");
        }

        if (petsDistintos < 0)
        {
            throw new RegraDeDominioException(
                $"Pets distintos não pode ser negativo (grupo {grupo}, valor {petsDistintos}).");
        }

        if (obrigacoesCumpridas > obrigacoesResolvidas)
        {
            throw new RegraDeDominioException(
                $"Coorte {grupo} tem {obrigacoesCumpridas} obrigações cumpridas para " +
                $"{obrigacoesResolvidas} resolvidas. O denominador da coorte são as obrigações " +
                "já resolvidas, então cumprida é sempre um subconjunto dele — um total maior " +
                "indica obrigação ainda não resolvida contada no numerador.");
        }

        return new Coorte(grupo, obrigacoesResolvidas, obrigacoesCumpridas, petsDistintos);
    }
}
