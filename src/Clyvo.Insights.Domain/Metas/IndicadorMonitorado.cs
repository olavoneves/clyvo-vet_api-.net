namespace Clyvo.Insights.Domain.Metas;

/// <summary>
/// Indicador da análise de coorte sobre o qual a clínica pode definir uma meta.
/// </summary>
/// <remarks>
/// Os dois são percentuais, e é por isso que o limiar tem uma faixa fechada de
/// 0 a 100. Admitir aqui um indicador em reais quebraria essa invariante e
/// obrigaria a meta a carregar unidade — que é exatamente a expansão que este
/// recorte evita.
/// </remarks>
public enum IndicadorMonitorado
{
    /// <summary>Percentual de obrigações resolvidas que foram cumpridas no grupo tratado.</summary>
    TaxaCumprimento = 1,

    /// <summary>Diferença entre as taxas dos dois braços, em pontos percentuais.</summary>
    DeltaPontosPercentuais = 2
}
