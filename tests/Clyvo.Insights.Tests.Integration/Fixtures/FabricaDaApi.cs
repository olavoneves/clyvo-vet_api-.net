using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Clyvo.Insights.Application.Abstracoes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Clyvo.Insights.Tests.Integration.Fixtures;

/// <summary>
/// Sobe a API inteira em processo, trocando só os repositórios.
/// </summary>
/// <remarks>
/// Tudo o que está sendo testado continua real: o middleware de exceção, a
/// validação do JWT, a policy de autorização, o roteamento, a serialização e os
/// casos de uso. O que sai é a ida ao banco.
/// </remarks>
public sealed class FabricaDaApi : WebApplicationFactory<Program>
{
    /// <summary>
    /// A mesma chave dos dois lados, como em produção. Aqui ela é de teste e
    /// nasce no código porque o segredo real nunca é versionado.
    /// </summary>
    public const string SegredoJwt = "chave-de-teste-do-clyvo-insights-com-mais-de-32-bytes";

    public ProjecaoRepositoryEmMemoria Projecoes { get; } = new();

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public FabricaDaApi()
    {
        // Definido antes de o host ser construído: o Program lê Jwt:Secret na
        // montagem do builder, ou seja, cedo demais para uma fonte de
        // configuração acrescentada pelo WebApplicationFactory.
        Environment.SetEnvironmentVariable("Jwt__Secret", SegredoJwt);
        Environment.SetEnvironmentVariable(
            "Mongo__ConnectionString", "mongodb://localhost:27017/?serverSelectionTimeoutMS=200");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ILeituraDoCoreRepository>();
            services.RemoveAll<IMetaIndicadorRepository>();
            services.RemoveAll<IProjecaoRepository>();

            services.AddSingleton<ILeituraDoCoreRepository, LeituraDoCoreRepositoryEmMemoria>();
            services.AddSingleton<IMetaIndicadorRepository, MetaIndicadorRepositoryEmMemoria>();
            services.AddSingleton<IProjecaoRepository>(Projecoes);
        });
    }

    /// <summary>Cliente autenticado como a clínica pedida.</summary>
    public HttpClient ClienteDaClinica(long idClinica)
    {
        var cliente = CreateClient();

        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenDaClinica(idClinica));

        return cliente;
    }

    /// <summary>
    /// Emite um token com a mesma forma que o clyvo-core emite: HS256 sobre os
    /// bytes UTF-8 do segredo, sem issuer nem audience, com o claim
    /// <c>idClinica</c>.
    /// </summary>
    public static string TokenDaClinica(long idClinica, string? segredo = null) =>
        Token(segredo ?? SegredoJwt, new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "colaborador@clyvo.test"),
            new("nome", "Colaborador de Teste"),
            new("tipo", "COLABORADOR"),
            new("idClinica", idClinica.ToString())
        });

    /// <summary>Token válido e assinado, mas sem o claim de tenant.</summary>
    public static string TokenSemClinica() =>
        Token(SegredoJwt, new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "colaborador@clyvo.test"),
            new("tipo", "COLABORADOR")
        });

    private static string Token(string segredo, List<Claim> claims)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(segredo));

        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
