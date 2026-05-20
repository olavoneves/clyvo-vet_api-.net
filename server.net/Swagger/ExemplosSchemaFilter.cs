using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using server.net.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace server.net.Swagger;

public class ExemplosSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(Tutor))
        {
            schema.Example = new OpenApiObject
            {
                ["id"] = new OpenApiInteger(0),
                ["nome"] = new OpenApiString("João Silva"),
                ["email"] = new OpenApiString("joao.silva@email.com"),
                ["telefone"] = new OpenApiString("(11) 99999-0001"),
                ["cpf"] = new OpenApiString("123.456.789-00")
            };
        }
        else if (context.Type == typeof(Pet))
        {
            schema.Example = new OpenApiObject
            {
                ["id"] = new OpenApiInteger(0),
                ["nome"] = new OpenApiString("Rex"),
                ["especie"] = new OpenApiString("Cachorro"),
                ["raca"] = new OpenApiString("Labrador"),
                ["dataNascimento"] = new OpenApiString("2021-06-10T00:00:00"),
                ["tutorId"] = new OpenApiInteger(1)
            };
        }
        else if (context.Type == typeof(Consulta))
        {
            schema.Example = new OpenApiObject
            {
                ["id"] = new OpenApiInteger(0),
                ["data"] = new OpenApiString("2026-05-20T09:00:00"),
                ["descricao"] = new OpenApiString("Check-up anual"),
                ["diagnostico"] = new OpenApiString("Animal saudável"),
                ["veterinarioNome"] = new OpenApiString("Dra. Ana Costa"),
                ["petId"] = new OpenApiInteger(1)
            };
        }
    }
}
