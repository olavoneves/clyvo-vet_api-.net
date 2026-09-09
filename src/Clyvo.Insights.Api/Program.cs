using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Clyvo.Insights.Api.Autenticacao;
using Clyvo.Insights.Api.Middlewares;
using Clyvo.Insights.Api.Observabilidade;
using Clyvo.Insights.Api.Swagger;
using Clyvo.Insights.Application;
using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

const string NomeDoServico = "clyvo-insights";

// O sufixo vazio antes da extensao e onde o Serilog encaixa a data: o arquivo
// do dia vira clyvo-insights-20260908.log.
const string ArquivoDeLog = "logs/clyvo-insights-.log";

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------------
// Log estruturado
//
// JSON em vez de texto: o log deste serviço existe para ser consultado depois,
// e "delta de 25,07 p.p. para a clínica 23" só vira filtro se IdClinica for um
// campo, e não parte de uma frase. O enricher de trace liga cada linha ao span
// do OpenTelemetry e ao traceId que o cliente recebeu no ProblemDetails.
//
// Dois destinos, com papéis diferentes: o console é o que o orquestrador
// coleta, e o arquivo é o que sobrevive ao container ser recriado durante a
// investigação de um incidente. Mesmo formato nos dois, para que a mesma
// consulta sirva aos dois.
// ----------------------------------------------------------------------------
builder.Host.UseSerilog((contexto, servicos, configuracao) => configuracao
    .ReadFrom.Configuration(contexto.Configuration)
    .ReadFrom.Services(servicos)
    .Enrich.FromLogContext()
    .Enrich.With(new EnriquecedorDeTrace())
    .Enrich.WithProperty("Servico", NomeDoServico)
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(
        new CompactJsonFormatter(),
        path: contexto.Configuration["Serilog:Arquivo"] ?? ArquivoDeLog,
        rollingInterval: RollingInterval.Day,
        // Uma semana. Log de análise de coorte não é registro clínico, e reter
        // indefinidamente só enche o disco de quem esquecer de limpar.
        retainedFileCountLimit: 7,
        shared: true));

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

// O 400 de validação de modelo é produzido pelo próprio [ApiController], antes
// de qualquer código nosso rodar, e por isso não passa pelo middleware de
// exceção. Sem este ajuste, o erro mais comum da API seria o único a sair sem
// correlação — justamente o que o cliente mais tem a reportar.
builder.Services.Configure<ApiBehaviorOptions>(opcoes =>
{
    var padrao = opcoes.InvalidModelStateResponseFactory;

    opcoes.InvalidModelStateResponseFactory = contexto =>
    {
        if (contexto.HttpContext.Items.TryGetValue(CorrelacaoDeRequisicao.ChaveNoContexto, out var correlacao)
            && padrao(contexto) is ObjectResult resultado
            && resultado.Value is ProblemDetails problema)
        {
            problema.Extensions["correlationId"] = correlacao;

            return resultado;
        }

        return padrao(contexto);
    };
});

// ----------------------------------------------------------------------------
// Tracing e métricas
//
// Exportador de console de propósito: um coletor que ninguém sobe é
// infraestrutura morta no compose. Trocar por OTLP é uma linha, no dia em que
// houver para onde exportar.
// ----------------------------------------------------------------------------
// Agrega o histograma que o ASP.NET Core já publica, para servir /metrics sem
// um coletor externo e sem medir a mesma coisa duas vezes.
builder.Services.AddSingleton<ColetorDeMetricas>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(recurso => recurso.AddService(
        serviceName: NomeDoServico,
        serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(opcoes =>
            // Health check bate a cada poucos segundos e produziria mais span
            // que requisição de verdade.
            opcoes.Filter = contexto => !contexto.Request.Path.StartsWithSegments("/health"))
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(opcoes => opcoes.SetDbStatementForText = true)
        .AddConsoleExporter())
    .WithMetrics(metricas => metricas
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddConsoleExporter());

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
    .SetDefaultPolicy(new AuthorizationPolicyBuilder()
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

// Resolvido agora, e não na primeira chamada a /metrics: o MeterListener começa
// a escutar quando o coletor é construído, e um singleton preguiçoso perderia
// tudo o que aconteceu antes de alguém abrir o endpoint.
app.Services.GetRequiredService<ColetorDeMetricas>();

// Primeiro na pipeline, antes até do tratamento de exceção: a correlação entra
// no LogContext aqui e continua ativa enquanto a exceção sobe. Na ordem
// inversa, o `using` do LogContext seria desfeito ao desempilhar, e a linha de
// erro — justamente a que se quer correlacionar — sairia sem a correlação.
app.UseMiddleware<CorrelacaoDeRequisicao>();

// Enxerga a exceção de tudo que vem depois.
app.UseMiddleware<ManipuladorGlobalDeExcecoes>();

// Uma linha por requisição, com rota, status e duração, em vez das três que o
// logger padrão do ASP.NET emite.
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clyvo Insights API v1"));

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Sem autenticação: quem sonda saúde é o orquestrador, que não tem token do
// core. Nenhum dos dois expõe dado de clínica.
app.MapHealthChecks("/health", RespostaDeSaude.Vivo).AllowAnonymous();
app.MapHealthChecks("/health/ready", RespostaDeSaude.Pronto).AllowAnonymous();

// Métricas de desempenho: duração por rota e respostas por faixa de status.
// Anônimo pela mesma razão dos health checks — quem raspa métrica é
// infraestrutura, não usuário. O que sai daqui são nomes de rota e contagens
// agregadas, nunca dado de clínica.
app.MapGet("/metrics", (ColetorDeMetricas coletor) => Results.Json(coletor.Ler()))
    .AllowAnonymous()
    .WithName("Metricas");

app.Run();

/// <summary>
/// Exposto para que <c>WebApplicationFactory</c> encontre o ponto de entrada
/// nos testes de integracao.
/// </summary>
public partial class Program;
