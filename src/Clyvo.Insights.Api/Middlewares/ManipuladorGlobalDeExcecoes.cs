using System.Diagnostics;
using Clyvo.Insights.Domain.Excecoes;
using Microsoft.AspNetCore.Mvc;

namespace Clyvo.Insights.Api.Middlewares;

/// <summary>
/// Traduz exceção em <c>ProblemDetails</c>, num lugar só.
/// </summary>
/// <remarks>
/// Sem isto, cada controller acaba com o próprio try/catch, e a forma do erro
/// passa a depender de quem escreveu o método. Aqui a resposta de erro é a
/// mesma em toda a API e carrega o <c>traceId</c>, que é o que liga a resposta
/// ao log estruturado e ao tracing.
/// </remarks>
public sealed class ManipuladorGlobalDeExcecoes
{
    private readonly RequestDelegate _proximo;
    private readonly ILogger<ManipuladorGlobalDeExcecoes> _log;

    public ManipuladorGlobalDeExcecoes(RequestDelegate proximo, ILogger<ManipuladorGlobalDeExcecoes> log)
    {
        _proximo = proximo;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _proximo(contexto);
        }
        catch (Exception excecao)
        {
            await EscreverProblemDetailsAsync(contexto, excecao);
        }
    }

    private async Task EscreverProblemDetailsAsync(HttpContext contexto, Exception excecao)
    {
        var (status, titulo, detalhe) = Traduzir(excecao);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _log.LogError(excecao, "Falha não tratada em {Metodo} {Caminho}",
                contexto.Request.Method, contexto.Request.Path);
        }
        else
        {
            _log.LogWarning(excecao, "Requisição rejeitada em {Metodo} {Caminho}: {Motivo}",
                contexto.Request.Method, contexto.Request.Path, detalhe);
        }

        if (contexto.Response.HasStarted)
        {
            // Resposta já em trânsito: sobrescrever produziria corpo corrompido.
            return;
        }

        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalhe,
            Instance = contexto.Request.Path
        };

        problema.Extensions["traceId"] = Activity.Current?.Id ?? contexto.TraceIdentifier;

        contexto.Response.Clear();
        contexto.Response.StatusCode = status;
        contexto.Response.ContentType = "application/problem+json";

        await contexto.Response.WriteAsJsonAsync(problema);
    }

    /// <summary>
    /// Violação de invariante do domínio é erro de quem chamou, e vira 400. O
    /// resto vira 500 sem detalhe, porque mensagem de exceção interna em corpo
    /// de resposta é vazamento de implementação.
    /// </summary>
    private static (int Status, string Titulo, string Detalhe) Traduzir(Exception excecao) => excecao switch
    {
        RegraDeDominioException regra =>
            (StatusCodes.Status400BadRequest, "Requisição inválida", regra.Message),

        _ => (StatusCodes.Status500InternalServerError, "Erro interno",
              "A requisição não pôde ser concluída. Consulte o traceId no log do serviço.")
    };
}
