using System.ComponentModel.DataAnnotations;
using Clyvo.Insights.Domain.Metas;

namespace Clyvo.Insights.Application.Metas;

/// <summary>Meta de indicador como sai na resposta.</summary>
/// <param name="Id">Identificador da meta.</param>
/// <param name="Indicador">Indicador monitorado.</param>
/// <param name="LimiarPercentual">Piso aceitável, de 0 a 100.</param>
/// <param name="DataCriacao">Quando a meta foi definida.</param>
/// <param name="DataAtualizacao">Quando o limiar mudou pela última vez.</param>
public sealed record MetaIndicadorDto(
    long Id,
    string Indicador,
    decimal LimiarPercentual,
    DateTime DataCriacao,
    DateTime DataAtualizacao);

/// <summary>Corpo de criação de meta.</summary>
/// <remarks>
/// Sem campo de clínica: o tenant sai do token. Aceitar aqui um id de clínica
/// deixaria o cliente escolher em que tenant escrever.
/// </remarks>
public sealed record CriarMetaIndicadorRequest
{
    /// <summary>TaxaCumprimento ou DeltaPontosPercentuais.</summary>
    [Required]
    public IndicadorMonitorado? Indicador { get; init; }

    /// <summary>Piso aceitável, de 0 a 100.</summary>
    [Required]
    [Range(0, 100)]
    public decimal? LimiarPercentual { get; init; }
}

/// <summary>Corpo de atualização de meta. Só o limiar muda.</summary>
public sealed record AtualizarMetaIndicadorRequest
{
    /// <summary>Novo piso aceitável, de 0 a 100.</summary>
    [Required]
    [Range(0, 100)]
    public decimal? LimiarPercentual { get; init; }
}

/// <summary>Uma meta confrontada com o valor que a análise apurou.</summary>
/// <param name="Indicador">Indicador monitorado.</param>
/// <param name="LimiarPercentual">Piso definido pela clínica.</param>
/// <param name="ValorApurado">Valor que a análise de coorte produziu.</param>
/// <param name="AbaixoDoLimiar">Se o apurado ficou abaixo do piso.</param>
public sealed record MetaAvaliadaDto(
    string Indicador,
    decimal LimiarPercentual,
    decimal ValorApurado,
    bool AbaixoDoLimiar);
