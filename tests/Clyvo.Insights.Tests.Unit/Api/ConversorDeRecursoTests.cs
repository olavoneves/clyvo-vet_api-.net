using System.Text.Json;
using Clyvo.Insights.Api.Hipermidia;
using Clyvo.Insights.Application.Metas;

namespace Clyvo.Insights.Tests.Unit.Api;

public class ConversorDeRecursoTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static Recurso<MetaIndicadorDto> Recurso() =>
        new(new MetaIndicadorDto(5, "TaxaCumprimento", 55m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
            new Dictionary<string, Link>
            {
                ["self"] = new("http://localhost/api/insights/metas/5"),
                ["remover"] = new("http://localhost/api/insights/metas/5", "DELETE")
            });

    [Fact]
    public void Serializar_UmRecurso_AchataOsCamposEPoe_linksAoLado()
    {
        // Act
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(Recurso(), Web));
        var raiz = json.RootElement;

        // Assert — os campos do DTO na raiz, e não dentro de "dados"
        Assert.Equal(5, raiz.GetProperty("id").GetInt64());
        Assert.Equal("TaxaCumprimento", raiz.GetProperty("indicador").GetString());
        Assert.False(raiz.TryGetProperty("dados", out _));

        var links = raiz.GetProperty("_links");
        Assert.Equal("http://localhost/api/insights/metas/5", links.GetProperty("self").GetProperty("href").GetString());
        Assert.False(links.GetProperty("self").TryGetProperty("method", out _), "GET não declara método.");
        Assert.Equal("DELETE", links.GetProperty("remover").GetProperty("method").GetString());
    }

    [Fact]
    public void Desserializar_OQueFoiSerializado_RecuperaDadosELinks()
    {
        // Arrange
        var original = Recurso();
        var texto = JsonSerializer.Serialize(original, Web);

        // Act
        var relido = JsonSerializer.Deserialize<Recurso<MetaIndicadorDto>>(texto, Web)!;

        // Assert
        Assert.Equal(original.Dados, relido.Dados);
        Assert.Equal(original.Links["remover"], relido.Links["remover"]);
        Assert.Equal(2, relido.Links.Count);
    }

    [Fact]
    public void Serializar_RecursoDeValorQueNaoEObjeto_Recusa()
    {
        // Arrange — um número não tem onde pendurar _links
        var recurso = new Recurso<int>(7, new Dictionary<string, Link>());

        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize(recurso, Web));
    }
}
