namespace Clyvo.Insights.Infrastructure.Mongo;

/// <summary>Conexão com o MongoDB das projeções.</summary>
public sealed class OpcoesMongo
{
    public const string Secao = "Mongo";

    /// <summary>
    /// String de conexão. Carrega credencial, então vem de variável de
    /// ambiente e não do appsettings versionado.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    public string Database { get; init; } = "clyvo_insights";

    /// <summary>Série de apurações, um documento por clínica e dia.</summary>
    public string ColecaoSnapshots { get; init; } = "snapshots_coorte";

    /// <summary>Log append-only de consulta a indicadores.</summary>
    public string ColecaoConsultas { get; init; } = "consultas_indicador";
}
