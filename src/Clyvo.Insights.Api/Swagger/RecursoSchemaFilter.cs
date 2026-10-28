using Clyvo.Insights.Api.Hipermidia;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Clyvo.Insights.Api.Swagger;

/// <summary>
/// Documenta <see cref="Recurso{T}"/> com a forma que ele tem no fio.
/// </summary>
/// <remarks>
/// O Swashbuckle lê o tipo por reflexão e documentaria <c>dados</c> e
/// <c>links</c> como propriedades — mas o conversor achata o DTO e escreve
/// <c>_links</c> ao lado. O schema vira a composição dos dois, que é o que o
/// cliente de fato recebe.
/// </remarks>
public sealed class RecursoSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsGenericType || context.Type.GetGenericTypeDefinition() != typeof(Recurso<>))
        {
            return;
        }

        var dados = context.SchemaGenerator.GenerateSchema(
            context.Type.GetGenericArguments()[0], context.SchemaRepository);

        var links = context.SchemaGenerator.GenerateSchema(
            typeof(IReadOnlyDictionary<string, Link>), context.SchemaRepository);

        schema.Properties.Clear();
        schema.Required.Clear();
        schema.AllOf = new List<OpenApiSchema>
        {
            dados,
            new()
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema> { [ConversorDeRecurso.PropriedadeDeLinks] = links }
            }
        };
    }
}
