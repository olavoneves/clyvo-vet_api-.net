using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Domain.Metas;

namespace Clyvo.Insights.Application.Mapeamentos;

/// <summary>Projeção da meta para a resposta. Escrita à mão, como o resto.</summary>
public static class MetaMapeamentos
{
    public static MetaIndicadorDto ParaDto(this MetaIndicador meta) =>
        new(meta.Id,
            meta.Indicador.ToString(),
            meta.LimiarPercentual,
            meta.DataCriacao,
            meta.DataAtualizacao);

    public static IReadOnlyList<MetaIndicadorDto> ParaDto(this IReadOnlyList<MetaIndicador> metas) =>
        metas.Select(ParaDto).ToList();

    /// <summary>Confronta a meta com o valor que a análise apurou.</summary>
    public static MetaAvaliadaDto Avaliar(this MetaIndicador meta, decimal valorApurado) =>
        new(meta.Indicador.ToString(),
            meta.LimiarPercentual,
            valorApurado,
            meta.EstaAbaixoDoLimiar(valorApurado));
}
