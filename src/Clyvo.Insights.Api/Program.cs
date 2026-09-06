using System.Reflection;
using System.Text.Json.Serialization;
using System.Text;
using Clyvo.Insights.Api.Autenticacao;
using Clyvo.Insights.Api.Middlewares;
using Clyvo.Insights.Api.Swagger;
using Clyvo.Insights.Application;
using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------------
// Camadas
// ----------------------------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContextHttp>();

// Enum entra e sai como nome. Ordinal em JSON obriga o cliente a saber que 1 é
// TaxaCumprimento, e transforma inserir um valor novo no meio do enum numa
// quebra silenciosa de contrato.
builder.Services.AddControllers().AddJsonOptions(json =>
    json.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ----------------------------------------------------------------------------
// Autenticação: mesma chave simétrica do clyvo-core
//
// O insights não emite token, só valida o que o core emitiu. O core assina com
// HS256 sobre os bytes UTF-8 do segredo e não preenche issuer nem audience —
// validar os dois recusaria todo token legítimo, então ficam desligados de
// propósito, com a assinatura e a validade fazendo o trabalho.
// ----------------------------------------------------------------------------
var opcoesJwt = builder.Configuration.GetSection(OpcoesJwt.Secao).Get<OpcoesJwt>() ?? new OpcoesJwt();

if (string.IsNullOrWhiteSpace(opcoesJwt.Secret))
{
    throw new InvalidOperationException(
        "Jwt:Secret não configurado. Ele é a mesma chave HMAC do clyvo-core (JWT_SECRET) e " +
        "precisa vir de variável de ambiente ou user-secrets — nunca do appsettings versionado. " +
        "Ex.: Jwt__Secret=<chave>.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcoesJwt.Secret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim(ClaimsDoCore.IdClinica)
        .Build());

// ----------------------------------------------------------------------------
// Swagger
// ----------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clyvo Insights API",
        Version = "v1",
        Description = "Leitura e analise de coorte do Clyvo Vet — FIAP Challenge 2026. " +
                      "Consome as views publicadas pelo clyvo-core e responde quanto da " +
                      "receita da clinica e atribuivel ao produto, e nao a inercia dos tutores."
    });

    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Access token emitido pelo clyvo-core em POST /api/auth/login. " +
                      "Cole só o token, sem o prefixo Bearer."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = JwtBearerDefaults.AuthenticationScheme
            }
        }] = Array.Empty<string>()
    });

    foreach (var xml in new[] { "Clyvo.Insights.Api.xml", "Clyvo.Insights.Application.xml" })
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, xml);
        if (File.Exists(caminho))
        {
            c.IncludeXmlComments(caminho);
        }
    }

    c.SchemaFilter<ExemplosSchemaFilter>();
});

var app = builder.Build();

// Primeiro na pipeline: precisa enxergar a exceção de tudo que vem depois.
app.UseMiddleware<ManipuladorGlobalDeExcecoes>();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clyvo Insights API v1"));

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Exposto para que <c>WebApplicationFactory</c> encontre o ponto de entrada
/// nos testes de integracao.
/// </summary>
public partial class Program;
