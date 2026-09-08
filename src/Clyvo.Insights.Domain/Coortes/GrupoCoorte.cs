namespace Clyvo.Insights.Domain.Coortes;

/// <summary>
/// Braço do experimento a que a obrigação pertence.
/// </summary>
/// <remarks>
/// O sorteio é determinístico por hash e acontece no motor, do lado Oracle. O
/// insights lê a flag já persistida — recalcular o sorteio na leitura criaria
/// uma segunda fonte de verdade que diverge da primeira no dia em que os
/// parâmetros mudarem.
/// </remarks>
public enum GrupoCoorte
{
    /// <summary>Pets perseguidos pelo motor de protocolos.</summary>
    Tratado = 1,

    /// <summary>Os ~10% deixados de fora de propósito, para servir de contrafactual.</summary>
    Controle = 2
}
