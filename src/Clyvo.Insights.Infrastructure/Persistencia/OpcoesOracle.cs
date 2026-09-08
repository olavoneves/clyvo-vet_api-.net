using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Clyvo.Insights.Infrastructure.Persistencia;

/// <summary>
/// Configuração do provider Oracle, num lugar só.
/// </summary>
/// <remarks>
/// A execução e as ferramentas de design precisam das mesmas opções. Duplicá-las
/// funcionaria até alguém mudar uma das duas — e a diferença só apareceria na
/// próxima migration.
/// </remarks>
public static class OpcoesOracle
{
    /// <summary>
    /// Histórico de migrations, também prefixado.
    /// </summary>
    /// <remarks>
    /// O padrão do EF é <c>__EFMigrationsHistory</c>, sem prefixo nenhum. Num
    /// schema Oracle compartilhado esse nome é de quem chegar primeiro: o
    /// segundo serviço .NET a rodar migration nele leria o histórico do
    /// primeiro e concluiria que já aplicou migrations que nunca viu.
    /// </remarks>
    public const string TabelaDeHistorico = "INS_EF_MIGRATIONS";

    public static DbContextOptionsBuilder ConfigurarOracle(
        this DbContextOptionsBuilder builder,
        string? connectionString) =>
        builder.UseOracle(connectionString, oracle => oracle
            .MigrationsHistoryTable(TabelaDeHistorico)
            .MigrationsAssembly(typeof(OpcoesOracle).Assembly.GetName().Name));
}
