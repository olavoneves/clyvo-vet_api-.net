using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using RedeIp = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace Clyvo.Insights.Api.Hipermidia;

/// <summary>
/// De quem a API aceita os cabeçalhos <c>X-Forwarded-*</c>.
/// </summary>
/// <remarks>
/// Os <c>_links</c> são montados com o esquema e o host da requisição, então
/// quem pode mandar <c>X-Forwarded-Host</c> escolhe o host dos links. A lista
/// vem de <c>Proxy:Confiaveis</c> — IPs (<c>10.0.0.5</c>) ou redes em CIDR
/// (<c>10.0.0.0/8</c>). Vazia, confia em qualquer origem, mas só em
/// desenvolvimento e nos testes; em qualquer outro ambiente fica o padrão do
/// ASP.NET, que aceita apenas o loopback.
/// </remarks>
public static class ProxiesConfiaveis
{
    public const string Secao = "Proxy:Confiaveis";

    public static void Configurar(
        ForwardedHeadersOptions opcoes,
        IReadOnlyCollection<string> confiaveis,
        bool ambienteDeDesenvolvimento)
    {
        opcoes.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        opcoes.ForwardLimit = 1;

        if (confiaveis.Count == 0)
        {
            if (ambienteDeDesenvolvimento)
            {
                opcoes.KnownNetworks.Clear();
                opcoes.KnownProxies.Clear();
            }

            return;
        }

        opcoes.KnownNetworks.Clear();
        opcoes.KnownProxies.Clear();

        foreach (var entrada in confiaveis)
        {
            var partes = entrada.Trim().Split('/');

            if (!IPAddress.TryParse(partes[0], out var endereco)
                || partes.Length > 2
                || (partes.Length == 2 && !int.TryParse(partes[1], out _)))
            {
                throw new InvalidOperationException(
                    $"{Secao} tem a entrada '{entrada}', que não é IP nem rede em CIDR (ex.: 10.0.0.0/8).");
            }

            if (partes.Length == 2)
            {
                opcoes.KnownNetworks.Add(new RedeIp(endereco, int.Parse(partes[1])));
            }
            else
            {
                opcoes.KnownProxies.Add(endereco);
            }
        }
    }
}
