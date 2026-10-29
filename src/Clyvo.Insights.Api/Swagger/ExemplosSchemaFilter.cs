using Clyvo.Insights.Api.Hipermidia;
using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Application.Obrigacoes;
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
        else if (context.Type == typeof(ObrigacaoDto))
        {
            schema.Example = ExemploDeObrigacao(comLinks: false);
        }
        else if (context.Type == typeof(Recurso<ObrigacaoDto>))
        {
            schema.Example = ExemploDeObrigacao(comLinks: true);
        }
        else if (context.Type == typeof(PaginaDeRecursos<ObrigacaoDto>))
        {
            schema.Example = ExemploDePagina();
        }
    }

    // Uma obrigação real da clínica 581 do banco de demonstração, e a página 2
    // da listagem das cumpridas dela, com os links que a navegação produz.
    private const string Base = "http://localhost:8081/api/insights/obrigacoes";

    private static OpenApiObject ExemploDeObrigacao(bool comLinks)
    {
        var exemplo = new OpenApiObject
        {
            ["id"] = new OpenApiLong(62016),
            ["idPet"] = new OpenApiLong(10722),
            ["nomePet"] = new OpenApiString("Rex"),
            ["nomeTutor"] = new OpenApiString("Silva Silva"),
            ["protocolo"] = new OpenApiString("Profilaxia dentaria felina"),
            ["categoria"] = new OpenApiString("ODONTO"),
            ["etapa"] = new OpenApiString("Avaliacao odontologica"),
            ["status"] = new OpenApiString("CUMPRIDA"),
            ["grupo"] = new OpenApiString("TRATADO"),
            ["dataPrevista"] = new OpenApiString("2025-05-05"),
            ["janelaInicio"] = new OpenApiString("2025-04-20"),
            ["janelaFim"] = new OpenApiString("2025-07-04"),
            ["valorEstimado"] = new OpenApiDouble(150),
            ["valorRealizado"] = new OpenApiDouble(150)
        };

        if (comLinks)
        {
            exemplo["_links"] = new OpenApiObject { ["self"] = Href($"{Base}/62016") };
        }

        return exemplo;
    }

    private static OpenApiObject ExemploDePagina()
    {
        static OpenApiObject Pagina(int numero) =>
            Href($"{Base}?pagina={numero}&tamanho=20&status=CUMPRIDA&ordenarPor=dataPrevista&direcao=asc");

        return new OpenApiObject
        {
            ["itens"] = new OpenApiArray { ExemploDeObrigacao(comLinks: true) },
            ["pagina"] = new OpenApiInteger(2),
            ["tamanho"] = new OpenApiInteger(20),
            ["totalItens"] = new OpenApiInteger(1759),
            ["totalPaginas"] = new OpenApiInteger(88),
            ["_links"] = new OpenApiObject
            {
                ["self"] = Pagina(2),
                ["first"] = Pagina(1),
                ["prev"] = Pagina(1),
                ["next"] = Pagina(3),
                ["last"] = Pagina(88)
            }
        };
    }

    private static OpenApiObject Href(string url) => new() { ["href"] = new OpenApiString(url) };

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
