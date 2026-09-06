namespace Clyvo.Insights.Application.Projecoes;

/// <summary>
/// Uma consulta a indicador, para o log.
/// </summary>
/// <remarks>
/// Serve para responder quem olhou o quê e quando — inclusive nas consultas em
/// que a análise saiu indisponível, que são justamente as que alguém vai
/// perguntar depois por que não apareceram no painel.
/// </remarks>
/// <param name="IdClinica">Clínica do token.</param>
/// <param name="Indicador">Indicador consultado.</param>
/// <param name="InstanteUtc">Momento da consulta.</param>
/// <param name="Disponivel">Se a análise pôde ser calculada.</param>
/// <param name="TraceId">Correlaciona o registro com o log estruturado e o tracing.</param>
public sealed record RegistroDeConsulta(
    long IdClinica,
    string Indicador,
    DateTime InstanteUtc,
    bool Disponivel,
    string? TraceId);
