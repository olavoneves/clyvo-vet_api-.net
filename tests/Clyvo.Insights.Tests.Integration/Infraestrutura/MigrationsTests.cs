using System.Text.RegularExpressions;
using Clyvo.Insights.Infrastructure;
using Clyvo.Insights.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Clyvo.Insights.Tests.Integration.Infraestrutura;

/// <summary>
/// O que as migrations fariam no banco, sem banco.
/// </summary>
/// <remarks>
/// O EF gera o script de uma migration executando o <c>Up</c> e o <c>Down</c>
/// dela contra o provider do Oracle, sem abrir conexão. É o mesmo SQL que o
/// <c>dotnet ef database update</c> mandaria — e o lugar certo para travar a
/// regra de que este serviço só cria o que é dele (<c>INS_</c>) e nunca
/// governa as views do core.
/// </remarks>
public class MigrationsTests
{
    private static InsightsDbContext Contexto()
    {
        var construtor = new DbContextOptionsBuilder<InsightsDbContext>();
        construtor.ConfigurarOracle("User Id=sem_banco;Data Source=localhost:1/TESTES_SEM_BANCO");

        return new InsightsDbContext(construtor.Options);
    }

    // O provider do Oracle embrulha o DDL em EXECUTE IMMEDIATE e quebra a linha
    // entre o comando e o nome da tabela: casa os dois, com o que houver no meio.
    private static Regex Ddl(string comando, string tabela) =>
        new(comando + @"\W+" + tabela + "\"", RegexOptions.IgnoreCase);

    [Fact]
    public void GenerateScript_DoZeroAteAUltima_CriaSoAsTabelasINSERegistraCadaMigration()
    {
        // Arrange
        using var contexto = Contexto();
        var migrador = contexto.GetService<IMigrator>();
        var migrations = contexto.Database.GetMigrations().ToList();

        // Act
        var script = migrador.GenerateScript();

        // Assert
        Assert.Matches(Ddl("CREATE TABLE", "INS_META_INDICADOR"), script);
        Assert.Matches(Ddl("CREATE TABLE", "INS_SNAPSHOT_COORTE"), script);
        Assert.DoesNotContain("VW_CLV_", script);

        foreach (var migration in migrations)
        {
            Assert.Contains(migration, script);
        }
    }

    [Fact]
    public void GenerateScript_DaUltimaAteOZero_DesfazAsTabelasINSSemTocarNasViews()
    {
        // Arrange
        using var contexto = Contexto();
        var migrador = contexto.GetService<IMigrator>();
        var ultima = contexto.Database.GetMigrations().Last();

        // Act — percorre o Down de todas, da mais nova para a mais antiga
        var script = migrador.GenerateScript(fromMigration: ultima, toMigration: Migration.InitialDatabase);

        // Assert
        Assert.Matches(Ddl("DROP TABLE", "INS_META_INDICADOR"), script);
        Assert.Matches(Ddl("DROP TABLE", "INS_SNAPSHOT_COORTE"), script);
        Assert.DoesNotContain("VW_CLV_", script);
    }

    [Fact]
    public void ModelSnapshot_ComparadoAoModeloAtual_NaoTemMudancaSemMigration()
    {
        // Arrange — o snapshot é o modelo como a última migration o deixou
        using var contexto = Contexto();
        IModel snapshot = contexto.GetService<IMigrationsAssembly>().ModelSnapshot!.Model;

        if (snapshot is IMutableModel mutavel)
        {
            snapshot = mutavel.FinalizeModel();
        }

        snapshot = contexto.GetService<IModelRuntimeInitializer>().Initialize(snapshot);
        var atual = contexto.GetService<IDesignTimeModel>().Model;

        // Act
        var diferencas = contexto.GetService<IMigrationsModelDiffer>()
            .GetDifferences(snapshot.GetRelationalModel(), atual.GetRelationalModel());

        // Assert — alterar entidade ou configuração sem gerar migration quebra aqui
        Assert.Empty(diferencas);
    }
}
