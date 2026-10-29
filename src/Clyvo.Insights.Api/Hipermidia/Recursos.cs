using System.Text.Json;
using System.Text.Json.Serialization;

namespace Clyvo.Insights.Api.Hipermidia;

/// <summary>Um link de hipermídia.</summary>
/// <param name="Href">URL absoluta, com o host e o esquema da requisição.</param>
/// <param name="Metodo">
/// Método HTTP, quando não é GET. Omitido em links de navegação: o cliente
/// segue com GET o que não diz o contrário.
/// </param>
public sealed record Link(
    string Href,
    [property: JsonPropertyName("method")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Metodo = null);

/// <summary>
/// Um recurso com os links do que se pode fazer a partir dele.
/// </summary>
/// <remarks>
/// <para>
/// Serializado <b>achatado</b>: os campos do DTO e, ao lado deles,
/// <c>_links</c>. O cliente lê <c>obrigacao.status</c>, e não
/// <c>obrigacao.dados.status</c>.
/// </para>
/// <para>
/// Os DTOs da Aplicação continuam sem saber de link. Hipermídia é forma de
/// representar o recurso em HTTP, e rota é assunto da Api; um DTO com
/// <c>_links</c> obrigaria a Aplicação a conhecer URL.
/// </para>
/// </remarks>
[JsonConverter(typeof(ConversorDeRecurso))]
public sealed class Recurso<T>
{
    public Recurso(T dados, IReadOnlyDictionary<string, Link> links)
    {
        Dados = dados;
        Links = links;
    }

    public T Dados { get; }

    public IReadOnlyDictionary<string, Link> Links { get; }
}

/// <summary>Uma coleção de recursos, com os links da coleção.</summary>
/// <param name="Itens">Os recursos, cada um com os próprios links.</param>
/// <param name="Links">Links da coleção: <c>self</c> e as ações sobre ela.</param>
public sealed record ColecaoDeRecursos<T>(
    IReadOnlyList<Recurso<T>> Itens,
    [property: JsonPropertyName("_links")] IReadOnlyDictionary<string, Link> Links);

/// <summary>Uma página de recursos, com os links de navegação.</summary>
/// <param name="Itens">Os recursos da página, cada um com o próprio <c>self</c>.</param>
/// <param name="Pagina">Número da página, a partir de 1.</param>
/// <param name="Tamanho">Itens por página pedidos.</param>
/// <param name="TotalItens">Total que atende ao filtro, em todas as páginas.</param>
/// <param name="TotalPaginas">Zero quando não há item nenhum.</param>
/// <param name="Links">
/// <c>self</c>, <c>first</c>, <c>prev</c>, <c>next</c> e <c>last</c>, todos com os
/// mesmos filtros e a mesma ordenação. <c>prev</c> some na primeira página e
/// <c>next</c> na última.
/// </param>
public sealed record PaginaDeRecursos<T>(
    IReadOnlyList<Recurso<T>> Itens,
    int Pagina,
    int Tamanho,
    int TotalItens,
    int TotalPaginas,
    [property: JsonPropertyName("_links")] IReadOnlyDictionary<string, Link> Links);

/// <summary>Serializa <see cref="Recurso{T}"/> achatado, com <c>_links</c> no fim.</summary>
public sealed class ConversorDeRecurso : JsonConverterFactory
{
    public const string PropriedadeDeLinks = "_links";

    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Recurso<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(
            typeof(Conversor<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;

    private sealed class Conversor<T> : JsonConverter<Recurso<T>>
    {
        public override Recurso<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var documento = JsonDocument.ParseValue(ref reader);

            var dados = documento.RootElement.Deserialize<T>(options)
                ?? throw new JsonException("Recurso sem dados.");

            var links = documento.RootElement.TryGetProperty(PropriedadeDeLinks, out var elemento)
                ? elemento.Deserialize<Dictionary<string, Link>>(options) ?? new Dictionary<string, Link>()
                : new Dictionary<string, Link>();

            return new Recurso<T>(dados, links);
        }

        public override void Write(Utf8JsonWriter writer, Recurso<T> value, JsonSerializerOptions options)
        {
            // Serializar o DTO com as mesmas opções da API — camelCase, enum
            // como texto — e copiar as propriedades dele para este objeto.
            using var dados = JsonSerializer.SerializeToDocument(value.Dados, options);

            if (dados.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException($"{typeof(T).Name} não serializa como objeto e não pode carregar links.");
            }

            writer.WriteStartObject();

            foreach (var propriedade in dados.RootElement.EnumerateObject())
            {
                propriedade.WriteTo(writer);
            }

            writer.WritePropertyName(PropriedadeDeLinks);
            JsonSerializer.Serialize(writer, value.Links, options);

            writer.WriteEndObject();
        }
    }
}
