using System.Diagnostics;
using Serilog.Context;

namespace Clyvo.Insights.Api.Observabilidade;

/// <summary>
/// Dá a cada requisição um identificador de correlação e o propaga.
/// </summary>
/// <remarks>
/// <para>
/// O identificador é o <c>TraceId</c> do OpenTelemetry, e não um GUID novo. Um
/// segundo identificador obrigaria quem investiga a cruzar duas numerações —
/// uma no log, outra no tracing — para reconstituir a mesma requisição.
/// </para>
/// <para>
/// Quando o chamador manda <c>X-Correlation-Id</c>, esse valor é respeitado: é
/// o caso do painel Java, que já carrega o seu ao chamar esta API, e da
/// investigação que começa do lado de lá. O valor recebido também é gravado
/// como tag no span, para que o trace apareça na busca por ele.
/// </para>
/// <para>
/// A correlação sai de volta no cabeçalho da resposta. Sem isso, o usuário que
/// relata um erro não tem o que informar, e a busca no log começa por adivinhar
/// qual das requisições daquele minuto era a dele.
/// </para>
/// </remarks>
public sealed class CorrelacaoDeRequisicao
{
    /// <summary>Cabeçalho de entrada e de saída da correlação.</summary>
    public const string Cabecalho = "X-Correlation-Id";

    /// <summary>Nome da propriedade no log estruturado e da tag no span.</summary>
    public const string Propriedade = "CorrelationId";

    /// <summary>
    /// Chave em <c>HttpContext.Items</c>. O middleware de exceção lê a
    /// correlação daqui, e não do cabeçalho de resposta: o cabeçalho só é
    /// escrito quando a resposta começa, e nesse momento o corpo do
    /// ProblemDetails já foi montado.
    /// </summary>
    public const string ChaveNoContexto = "clyvo.correlationId";

    private readonly RequestDelegate _proximo;

    public CorrelacaoDeRequisicao(RequestDelegate proximo)
    {
        _proximo = proximo;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        var correlacao = Resolver(contexto);

        contexto.Items[ChaveNoContexto] = correlacao;

        Activity.Current?.SetTag(Propriedade, correlacao);

        contexto.Response.OnStarting(() =>
        {
            contexto.Response.Headers[Cabecalho] = correlacao;

            return Task.CompletedTask;
        });

        // Empurrado no LogContext: toda linha emitida enquanto esta requisição
        // estiver em curso carrega a correlação, inclusive a linha de conclusão
        // do UseSerilogRequestLogging, que é registrado depois deste middleware.
        using (LogContext.PushProperty(Propriedade, correlacao))
        {
            await _proximo(contexto);
        }
    }

    private static string Resolver(HttpContext contexto)
    {
        var recebida = contexto.Request.Headers[Cabecalho].ToString();

        if (!string.IsNullOrWhiteSpace(recebida))
        {
            // Limite defensivo: o valor vai para o log e para o cabeçalho de
            // resposta, e cabeçalho de entrada é dado do cliente.
            return recebida.Length > 128 ? recebida[..128] : recebida;
        }

        return Activity.Current?.TraceId.ToString() ?? contexto.TraceIdentifier;
    }
}
