using Clyvo.Insights.Application.Coortes;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Clyvo.Insights.Api.Swagger;

/// <summary>
/// Preenche os exemplos do Swagger com números reais.
/// </summary>
/// <remarks>
/// Portado do projeto anterior. Exemplo com valor plausível economiza a ida ao
/// banco só para descobrir a forma da resposta — e no caso da coorte ele já
/// mostra a leitura certa: 25 pontos percentuais de diferença entre tratado e
/// controle é o efeito, não a taxa.
/// </remarks>
public sealed class ExemplosSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(AnaliseCoorteDto))
        {
            schema.Example = new OpenApiObject
            {
                ["idClinica"] = new OpenApiLong(23),
                ["disponivel"] = new OpenApiBoolean(true),
                ["motivoIndisponibilidade"] = new OpenApiNull(),
                ["deltaPontosPercentuais"] = new OpenApiDouble(25.07),
                ["consultasAtribuiveis"] = new OpenApiDouble(680.66),
                ["receitaRecuperada"] = new OpenApiDouble(138671.07),
                ["receitaEstimavel"] = new OpenApiBoolean(true),
                ["ticketMedio"] = new OpenApiDouble(203.73),
                ["tratado"] = ExemploDeCoorte("TRATADO", 2715, 1601, 58.97, 218),
                ["controle"] = ExemploDeCoorte("CONTROLE", 354, 120, 33.90, 27)
            };
        }
        else if (context.Type == typeof(CoorteDto))
        {
            schema.Example = ExemploDeCoorte("TRATADO", 2715, 1601, 58.97, 218);
        }
    }

    private static OpenApiObject ExemploDeCoorte(
        string grupo,
        int resolvidas,
        int cumpridas,
        double taxa,
        int pets) =>
        new()
        {
            ["grupo"] = new OpenApiString(grupo),
            ["obrigacoesResolvidas"] = new OpenApiInteger(resolvidas),
            ["obrigacoesCumpridas"] = new OpenApiInteger(cumpridas),
            ["taxaCumprimentoPercentual"] = new OpenApiDouble(taxa),
            ["petsDistintos"] = new OpenApiInteger(pets)
        };
}
