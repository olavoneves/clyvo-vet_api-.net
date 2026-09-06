using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Clyvo.Insights.Api.Observabilidade;

/// <summary>
/// Escreve o resultado do health check como JSON.
/// </summary>
/// <remarks>
/// O padrão do ASP.NET devolve só a palavra "Healthy" em texto puro, que diz
/// que algo quebrou sem dizer o quê. Aqui cada verificação aparece com seu
/// status, sua descrição e quanto demorou.
/// </remarks>
public static class RespostaDeSaude
{
    public static Task EscreverAsync(HttpContext contexto, HealthReport relatorio)
    {
        contexto.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = relatorio.Status.ToString(),
            duracaoMs = Math.Round(relatorio.TotalDuration.TotalMilliseconds, 1),
            verificacoes = relatorio.Entries.Select(entrada => new
            {
                nome = entrada.Key,
                status = entrada.Value.Status.ToString(),
                descricao = entrada.Value.Description,
                duracaoMs = Math.Round(entrada.Value.Duration.TotalMilliseconds, 1),
                // A exceção vira só a mensagem: /health costuma ser exposto sem
                // autenticação, e stack trace ali é mapa da implementação.
                erro = entrada.Value.Exception?.Message
            })
        };

        return contexto.Response.WriteAsync(JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Vivo: o processo responde. Não toca em dependência externa.</summary>
    /// <remarks>
    /// Se <c>/health</c> desse unhealthy por Oracle fora, o orquestrador
    /// reiniciaria um processo saudável em vez de esperar o banco voltar — e o
    /// reinício não traz o banco de volta.
    /// </remarks>
    public static readonly HealthCheckOptions Vivo = new()
    {
        Predicate = verificacao => verificacao.Tags.Contains("live"),
        ResponseWriter = EscreverAsync
    };

    /// <summary>Pronto: as dependências que o serviço precisa para servir respondem.</summary>
    public static readonly HealthCheckOptions Pronto = new()
    {
        Predicate = verificacao => verificacao.Tags.Contains("ready"),
        ResponseWriter = EscreverAsync
    };
}
