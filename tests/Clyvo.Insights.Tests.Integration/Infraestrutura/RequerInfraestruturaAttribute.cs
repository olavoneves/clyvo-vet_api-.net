namespace Clyvo.Insights.Tests.Integration.Infraestrutura;

/// <summary>
/// Marca um teste que só roda contra Oracle e MongoDB de verdade.
/// </summary>
/// <remarks>
/// <para>
/// Equivalente ao <c>@EnabledIfEnvironmentVariable</c> que o repositório Java
/// usa: sem a variável, o teste aparece como ignorado em vez de falhar. Um
/// clone limpo, sem banco nenhum, precisa ter a suíte verde — caso contrário o
/// vermelho deixa de significar "quebrou" e passa a significar "não configurei".
/// </para>
/// <para>
/// Habilite com:
/// <code>
/// CLYVO_TESTES_DE_INFRA=1
/// ConnectionStrings__Oracle="User Id=petflow;Password=...;Data Source=localhost:1521/PETFLOWDB"
/// Mongo__ConnectionString="mongodb://insights:...@localhost:27017/?authSource=admin"
/// </code>
/// </para>
/// </remarks>
public sealed class RequerInfraestruturaAttribute : FactAttribute
{
    public const string Variavel = "CLYVO_TESTES_DE_INFRA";

    public RequerInfraestruturaAttribute()
    {
        if (!Habilitado)
        {
            Skip = $"Defina {Variavel}=1 para rodar os testes que exigem Oracle e MongoDB.";
        }
    }

    public static bool Habilitado =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variavel));
}
