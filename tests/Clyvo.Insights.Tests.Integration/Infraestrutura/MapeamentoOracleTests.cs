using Clyvo.Insights.Domain.Metas;
using Clyvo.Insights.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Clyvo.Insights.Tests.Integration.Infraestrutura;

/// <summary>
/// Mapeamento EF contra o Oracle de verdade.
/// </summary>
/// <remarks>
/// Estes testes existem por causa de uma classe de defeito específica: a que só
/// aparece na ida e volta ao banco. O <c>DateTimeKind</c> perdido no
/// <c>TIMESTAMP</c> do Oracle passou despercebido por toda a suíte de dublês —
/// em memória a data nunca perde o fuso, porque nunca sai do processo. O que se
/// afirma aqui é <b>forma e mapeamento</b>, nunca contagem: o seed usa
/// <c>SYS_GUID</c> e datas relativas a <c>SYSDATE</c>, e teste que fixa contagem
/// quebra sozinho na próxima reexecução.
/// </remarks>
[Collection(ColecaoDeInfraestruturaReal.Nome)]
public class MapeamentoOracleTests
{
    private readonly InfraestruturaRealFixture _infra;

    public MapeamentoOracleTests(InfraestruturaRealFixture infra)
    {
        _infra = infra;
    }

    [RequerInfraestrutura]
    public async Task SalvarAsync_ComMetaNova_DevolveDataEmUtcAoRelerDoOracle()
    {
        // Arrange — uma clínica que não existe no seed, para não colidir com a
        // unique de (clínica, indicador) nem sujar dado de demonstração
        const long clinicaDeTeste = 999_001;
        var meta = MetaIndicador.Criar(
            clinicaDeTeste, IndicadorMonitorado.DeltaPontosPercentuais, 62.5m, DateTime.UtcNow);

        await using (var escrita = _infra.CriarContexto())
        {
            await escrita.Metas.AddAsync(meta);
            await escrita.SaveChangesAsync();
        }

        try
        {
            // Act — contexto novo de propósito: reaproveitar o anterior devolveria
            // a instância rastreada, sem passar pelo banco, e o teste não provaria
            // nada sobre a ida e volta.
            await using var leitura = _infra.CriarContexto();
            var relida = await leitura.Metas.SingleAsync(m => m.IdClinica == clinicaDeTeste);

            // Assert
            Assert.Equal(DateTimeKind.Utc, relida.DataCriacao.Kind);
            Assert.Equal(DateTimeKind.Utc, relida.DataAtualizacao.Kind);
            Assert.Equal(62.5m, relida.LimiarPercentual);
            Assert.Equal(IndicadorMonitorado.DeltaPontosPercentuais, relida.Indicador);
            Assert.True(relida.Id > 0, "O identity do Oracle deveria ter atribuído o id.");
        }
        finally
        {
            await using var limpeza = _infra.CriarContexto();
            await limpeza.Metas.Where(m => m.IdClinica == clinicaDeTeste).ExecuteDeleteAsync();
        }
    }

    [RequerInfraestrutura]
    public async Task PainelCoorte_LidaDoOracle_TemAFormaDoContratoPublicado()
    {
        // Arrange
        await using var contexto = _infra.CriarContexto();

        // Act
        var linhas = await contexto.PainelCoorte.ToListAsync();

        // Assert — forma, não contagem
        Assert.NotEmpty(linhas);

        foreach (var linha in linhas)
        {
            Assert.Contains(linha.Grupo, new[] { "TRATADO", "CONTROLE" });
            Assert.True(linha.IdClinica > 0);
            Assert.True(linha.ObrigacoesResolvidas >= linha.ObrigacoesCumpridas,
                $"Clínica {linha.IdClinica}/{linha.Grupo}: cumpridas não podem passar de resolvidas.");
            Assert.True(linha.PetsDistintos > 0);
            Assert.True(linha.TicketMedio is null or > 0m,
                "Ticket médio, quando presente, é o valor apurado das consultas e não pode ser negativo.");
        }
    }

    [RequerInfraestrutura]
    public async Task PainelReceita_LidaDoOracle_TemFunilMonotonicamenteDecrescente()
    {
        // Arrange
        await using var contexto = _infra.CriarContexto();

        // Act
        var linhas = await contexto.PainelReceita.ToListAsync();

        // Assert — a invariante que o domínio assume ao converter funil em baldes
        Assert.NotEmpty(linhas);

        foreach (var linha in linhas)
        {
            Assert.True(linha.AlcancaramNotificada >= linha.AlcancaramRespondida);
            Assert.True(linha.AlcancaramRespondida >= linha.AlcancaramAgendada);
            Assert.True(linha.AlcancaramAgendada >= linha.Cumpridas);
            Assert.True(linha.Total >= linha.AlcancaramNotificada + linha.Perdidas,
                $"Clínica {linha.IdClinica}: notificadas e perdidas são disjuntas e não podem passar do total.");
        }
    }

    [RequerInfraestrutura]
    public async Task Migrations_NoOracle_EstaoAplicadasESemPendencia()
    {
        // Arrange
        await using var contexto = _infra.CriarContexto();

        // Act
        var pendentes = await contexto.Database.GetPendingMigrationsAsync();
        var aplicadas = (await contexto.Database.GetAppliedMigrationsAsync()).ToList();

        // Assert
        Assert.Empty(pendentes);
        Assert.Contains(aplicadas, m => m.EndsWith("CriaMetaIndicador", StringComparison.Ordinal));
        Assert.Contains(aplicadas, m => m.EndsWith("CriaSnapshotCoorte", StringComparison.Ordinal));
    }

    [RequerInfraestrutura]
    public async Task Migrations_NoOracle_NaoGovernamAsViewsDoCore()
    {
        // Arrange — o script completo do modelo é o que o EF emitiria do zero
        await using var contexto = _infra.CriarContexto();

        // Act
        var script = contexto.Database.GenerateCreateScript();

        // Assert — as tabelas próprias entram; as views do core, não
        Assert.Contains("INS_META_INDICADOR", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INS_SNAPSHOT_COORTE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("VW_CLV_PAINEL_COORTE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("VW_CLV_PAINEL_RECEITA", script, StringComparison.OrdinalIgnoreCase);

        await Task.CompletedTask;
    }
}
