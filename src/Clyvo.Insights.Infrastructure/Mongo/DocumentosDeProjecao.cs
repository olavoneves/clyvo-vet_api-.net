using Clyvo.Insights.Application.Coortes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Clyvo.Insights.Infrastructure.Mongo;

/// <summary>
/// O snapshot como documento.
/// </summary>
/// <remarks>
/// <c>Analise</c> guarda a resposta inteira, incluindo as metas avaliadas — e é
/// justamente essa parte que muda de forma: a clínica que não definiu meta
/// nenhuma grava uma lista vazia, a que definiu duas grava duas, e um indicador
/// novo entra sem tocar em schema. É essa variação que justifica o Mongo aqui,
/// e não velocidade de leitura.
/// </remarks>
public sealed class DocumentoSnapshotCoorte
{
    /// <summary>
    /// <c>{idClinica}:{aaaa-MM-dd}</c>. Chave natural: torna a regressão de
    /// "um ponto por dia" responsabilidade do banco, e não de quem chama.
    /// </summary>
    [BsonId]
    public string Id { get; init; } = string.Empty;

    public long IdClinica { get; init; }

    public string DataReferencia { get; init; } = string.Empty;

    public DateTime ApuradoEmUtc { get; init; }

    public AnaliseCoorteDto? Analise { get; init; }
}

/// <summary>Uma linha do log de consulta.</summary>
public sealed class DocumentoConsultaIndicador
{
    [BsonId]
    public ObjectId Id { get; init; }

    public long IdClinica { get; init; }

    public string Indicador { get; init; } = string.Empty;

    public DateTime InstanteUtc { get; init; }

    public bool Disponivel { get; init; }

    public string? TraceId { get; init; }
}
