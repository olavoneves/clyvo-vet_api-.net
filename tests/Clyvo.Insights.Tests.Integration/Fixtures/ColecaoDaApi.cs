namespace Clyvo.Insights.Tests.Integration.Fixtures;

/// <summary>
/// Uma instância só da API para todas as classes de teste que apenas leem.
/// </summary>
/// <remarks>
/// <para>
/// Subir o host é a parte cara desta suíte: DI, EF, MongoClient, OpenTelemetry
/// e Serilog são montados a cada <c>WebApplicationFactory</c>. Com
/// <c>IClassFixture</c> em três classes, isso acontecia três vezes; com a
/// collection, uma.
/// </para>
/// <para>
/// A collection também serializa a execução das classes que a compartilham, o
/// que é exatamente o que se quer de um host único e mutável — o xUnit roda
/// collections diferentes em paralelo, e duas classes escrevendo no mesmo host
/// ao mesmo tempo produziriam falha intermitente.
/// </para>
/// <para>
/// <c>MetasEndpointTests</c> fica de fora de propósito: ela cria e apaga metas,
/// e por isso mantém <c>IClassFixture</c> — host próprio, isolado de quem só lê.
/// </para>
/// </remarks>
[CollectionDefinition(Nome)]
public sealed class ColecaoDaApi : ICollectionFixture<FabricaDaApi>
{
    public const string Nome = "API do Clyvo Insights";
}
