using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Clyvo.Insights.Api.Observabilidade;

/// <summary>
/// Carimba <c>TraceId</c> e <c>SpanId</c> em cada evento de log.
/// </summary>
/// <remarks>
/// É o que liga as três superfícies de observabilidade: o mesmo TraceId aparece
/// no log estruturado, no span do OpenTelemetry e no corpo do ProblemDetails que
/// o cliente recebeu. Sem ele, investigar um erro relatado por um usuário
/// começa por adivinhar qual das requisições daquele minuto era a dele.
///
/// Escrito à mão em vez de trazer um pacote de enricher: são vinte linhas e uma
/// dependência a menos para justificar.
/// </remarks>
public sealed class EnriquecedorDeTrace : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var atividade = Activity.Current;

        if (atividade is null)
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("TraceId", atividade.TraceId.ToString()));

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("SpanId", atividade.SpanId.ToString()));
    }
}
