using Clyvo.Insights.Api.Hipermidia;
using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Application.Metas;
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
            schema.Example = ExemploDeAnalise(comLinks: false);
        }
        else if (context.Type == typeof(Recurso<AnaliseCoorteDto>))
        {
            schema.Example = ExemploDeAnalise(comLinks: true);
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
        else if (context.Type == typeof(ResumoObrigacoesDto))
        {
            schema.Example = ExemploDeResumo(comLinks: false);
        }
        else if (context.Type == typeof(Recurso<ResumoObrigacoesDto>))
        {
            schema.Example = ExemploDeResumo(comLinks: true);
        }
        else if (context.Type == typeof(MetaIndicadorDto))
        {
            schema.Example = ExemploDeMeta(61, "TaxaCumprimento", 55, comLinks: false);
        }
        else if (context.Type == typeof(Recurso<MetaIndicadorDto>))
        {
            schema.Example = ExemploDeMeta(61, "TaxaCumprimento", 55, comLinks: true);
        }
        else if (context.Type == typeof(ColecaoDeRecursos<MetaIndicadorDto>))
        {
            schema.Example = ExemploDeColecaoDeMetas();
        }
    }

    // Uma obrigação real da clínica 581 do banco de demonstração, e a página 2
    // da listagem das cumpridas dela, com os links que a navegação produz.
    private const string Raiz = "http://localhost:8081/api/insights";
    private const string Base = Raiz + "/obrigacoes";

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

    private static OpenApiObject Href(string url, string metodo) =>
        new() { ["href"] = new OpenApiString(url), ["method"] = new OpenApiString(metodo) };

    private static OpenApiObject ExemploDeAnalise(bool comLinks)
    {
        var exemplo = new OpenApiObject
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

        if (comLinks)
        {
            exemplo["_links"] = new OpenApiObject
            {
                ["self"] = Href($"{Raiz}/coorte"),
                ["obrigacoesTratamento"] = Href($"{Base}?grupo=tratamento"),
                ["obrigacoesControle"] = Href($"{Base}?grupo=controle")
            };
        }

        return exemplo;
    }

    // O resumo da clínica 581 no banco de demonstração, com um link por balde:
    // cada um abre a listagem das obrigações vencidas que pararam naquele estado.
    private static OpenApiObject ExemploDeResumo(bool comLinks)
    {
        var exemplo = new OpenApiObject
        {
            ["idClinica"] = new OpenApiLong(581),
            ["total"] = new OpenApiInteger(3083),
            ["cumpridas"] = new OpenApiInteger(1759),
            ["pararamEmAgendada"] = new OpenApiInteger(0),
            ["pararamEmRespondida"] = new OpenApiInteger(0),
            ["pararamEmNotificada"] = new OpenApiInteger(11),
            ["perdidas"] = new OpenApiInteger(1310),
            ["naoTrabalhadas"] = new OpenApiInteger(3),
            ["valorRecuperado"] = new OpenApiDouble(244405),
            ["valorPerdido"] = new OpenApiDouble(180500),
            ["mesInicio"] = new OpenApiString("2025-05-01T00:00:00"),
            ["mesFim"] = new OpenApiString("2026-10-01T00:00:00")
        };

        if (comLinks)
        {
            static OpenApiObject Balde(string status) => Href($"{Base}?status={status}&ate=2026-10-27");

            exemplo["_links"] = new OpenApiObject
            {
                ["self"] = Href($"{Base}/resumo"),
                ["cumpridas"] = Balde("CUMPRIDA"),
                ["pararamEmAgendada"] = Balde("AGENDADA"),
                ["pararamEmRespondida"] = Balde("RESPONDIDA"),
                ["pararamEmNotificada"] = Balde("NOTIFICADA"),
                ["perdidas"] = Balde("PERDIDA"),
                ["naoTrabalhadasPrevistas"] = Balde("PREVISTA"),
                ["naoTrabalhadasCanceladas"] = Balde("CANCELADA")
            };
        }

        return exemplo;
    }

    // Item e coleção de metas com as ações que cada um oferece: o método vai no
    // link, para o cliente não ter de adivinhar qual verbo a rota aceita.
    private static OpenApiObject ExemploDeMeta(long id, string indicador, double limiar, bool comLinks)
    {
        var exemplo = new OpenApiObject
        {
            ["id"] = new OpenApiLong(id),
            ["indicador"] = new OpenApiString(indicador),
            ["limiarPercentual"] = new OpenApiDouble(limiar),
            ["dataCriacao"] = new OpenApiString("2026-10-28T23:41:07Z"),
            ["dataAtualizacao"] = new OpenApiString("2026-10-28T23:41:07Z")
        };

        if (comLinks)
        {
            var self = $"{Raiz}/metas/{id}";

            exemplo["_links"] = new OpenApiObject
            {
                ["self"] = Href(self),
                ["atualizar"] = Href(self, "PUT"),
                ["remover"] = Href(self, "DELETE")
            };
        }

        return exemplo;
    }

    private static OpenApiObject ExemploDeColecaoDeMetas() =>
        new()
        {
            ["itens"] = new OpenApiArray
            {
                ExemploDeMeta(61, "TaxaCumprimento", 55, comLinks: true),
                ExemploDeMeta(62, "DeltaPontosPercentuais", 10, comLinks: true)
            },
            ["_links"] = new OpenApiObject
            {
                ["self"] = Href($"{Raiz}/metas"),
                ["criar"] = Href($"{Raiz}/metas", "POST")
            }
        };

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
