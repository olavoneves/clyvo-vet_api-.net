using System.Reflection;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

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

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clyvo Insights API v1"));

app.MapControllers();

app.Run();

/// <summary>
/// Exposto para que <c>WebApplicationFactory</c> encontre o ponto de entrada
/// nos testes de integracao.
/// </summary>
public partial class Program;
