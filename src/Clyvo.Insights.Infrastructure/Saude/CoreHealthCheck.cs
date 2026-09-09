using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Clyvo.Insights.Infrastructure.Saude;

/// <summary>
/// Disponibilidade do clyvo-core, o serviço externo do qual este depende.
/// </summary>
/// <remarks>
/// <para>
/// A dependência é indireta e vale a pena ser explícita: o insights não chama o
/// core para responder requisição nenhuma — ele valida localmente, com a chave
/// compartilhada, o token que o core emitiu. Logo, com o core fora, quem já tem
/// token válido continua sendo atendido normalmente até o token expirar.
/// </para>
/// <para>
/// É por isso que a falha aqui é <c>Degraded</c> e não <c>Unhealthy</c>.
/// Marcá-la como unhealthy tiraria de serviço uma instância que ainda atende, e
/// pior: numa queda do core, tiraria também a única API que ainda conseguiria
/// responder alguma coisa.
/// </para>
/// <para>
/// O que o degraded avisa é real e vale o sinal: enquanto o core estiver fora,
/// nenhum token novo é emitido, então a janela de atendimento do insights está
/// contando para acabar.
/// </para>
/// </remarks>
internal sealed class CoreHealthCheck : IHealthCheck
{
    /// <summary>Nome do <c>HttpClient</c> nomeado usado pela sonda.</summary>
    public const string NomeDoCliente = "clyvo-core";

    private readonly IHttpClientFactory _fabrica;
    private readonly OpcoesCore _opcoes;

    public CoreHealthCheck(IHttpClientFactory fabrica, OpcoesCore opcoes)
    {
        _fabrica = fabrica;
        _opcoes = opcoes;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_opcoes.BaseUrl))
        {
            return HealthCheckResult.Degraded(
                "Core:BaseUrl não configurado. A sonda de disponibilidade do clyvo-core está desligada.");
        }

        var cronometro = Stopwatch.StartNew();

        try
        {
            using var cliente = _fabrica.CreateClient(NomeDoCliente);

            // Timeout próprio, somado ao do cliente: cancela a espera sem
            // depender de o servidor fechar a conexão.
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limite.CancelAfter(TimeSpan.FromSeconds(_opcoes.TimeoutSegundos));

            using var resposta = await cliente.GetAsync(_opcoes.CaminhoDeSaude, limite.Token);

            cronometro.Stop();

            if (!resposta.IsSuccessStatusCode)
            {
                return HealthCheckResult.Degraded(
                    $"clyvo-core respondeu {(int)resposta.StatusCode} em {_opcoes.CaminhoDeSaude}. " +
                    "Tokens já emitidos continuam válidos; novos logins não.");
            }

            return HealthCheckResult.Healthy(
                $"clyvo-core respondeu em {cronometro.ElapsedMilliseconds} ms.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded(
                $"clyvo-core não respondeu em {_opcoes.TimeoutSegundos}s. " +
                "Tokens já emitidos continuam válidos; novos logins não.");
        }
        catch (Exception excecao)
        {
            return HealthCheckResult.Degraded(
                "clyvo-core inalcançável. Tokens já emitidos continuam válidos; novos logins não.",
                excecao);
        }
    }
}
