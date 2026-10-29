using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Domain.Metas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Clyvo.Insights.Tests.Integration.Infraestrutura;

/// <summary>
/// Os repositórios de leitura do core e de metas, e as sondas de prontidão,
/// contra o Oracle e o Mongo de verdade.
/// </summary>
/// <remarks>
/// Tudo resolvido pelo <c>AddInfrastructure</c>, como na API. Como no resto do
/// grupo, nada de contagem fixa: o que se afirma é coerência — o funil só
/// afunila, cada clínica só vê o que é dela — e a ida e volta da escrita.
/// </remarks>
[Collection(ColecaoDeInfraestruturaReal.Nome)]
public class RepositoriosOracleTests
{
    // Fora do seed, para não colidir com a unique de (clínica, indicador) nem
    // sujar dado de demonstração. Diferente da usada em MapeamentoOracleTests.
    private const long ClinicaDeTeste = 999_002;

    private readonly InfraestruturaRealFixture _infra;

    public RepositoriosOracleTests(InfraestruturaRealFixture infra)
    {
        _infra = infra;
    }

    [RequerInfraestrutura]
    public async Task ObterCoortesDaClinicaAsync_NaViewReal_DevolveSoOsDoisBracosConhecidos()
    {
        // Arrange
        await using var provedor = _infra.CriarProvedor();
        await using var escopo = provedor.CreateAsyncScope();
        var idClinica = await ClinicaComCoorteAsync();
        var repositorio = escopo.ServiceProvider.GetRequiredService<ILeituraDoCoreRepository>();

        // Act
        var linhas = await repositorio.ObterCoortesDaClinicaAsync(idClinica);

        // Assert
        Assert.NotEmpty(linhas);
        Assert.All(linhas, l =>
        {
            Assert.Contains(l.Grupo, new[] { GrupoCoorte.Tratado, GrupoCoorte.Controle });
            Assert.InRange(l.ObrigacoesCumpridas, 0, l.ObrigacoesResolvidas);
        });
        Assert.Equal(linhas.Count, linhas.Select(l => l.Grupo).Distinct().Count());
    }

    [RequerInfraestrutura]
    public async Task ObterFunilDaClinicaAsync_NaViewReal_CadaEtapaCabeNaAnterior()
    {
        // Arrange
        await using var provedor = _infra.CriarProvedor();
        await using var escopo = provedor.CreateAsyncScope();
        var idClinica = await ClinicaComCoorteAsync();
        var repositorio = escopo.ServiceProvider.GetRequiredService<ILeituraDoCoreRepository>();

        // Act
        var funil = await repositorio.ObterFunilDaClinicaAsync(idClinica);

        // Assert — o funil da VW_CLV_PAINEL_RECEITA é cumulativo
        Assert.True(funil.Total > 0);
        Assert.InRange(funil.AlcancaramNotificada, 0, funil.Total);
        Assert.InRange(funil.AlcancaramRespondida, 0, funil.AlcancaramNotificada);
        Assert.InRange(funil.AlcancaramAgendada, 0, funil.AlcancaramRespondida);
        Assert.InRange(funil.Cumpridas, 0, funil.AlcancaramAgendada);
        Assert.InRange(funil.Cumpridas + funil.Perdidas, 0, funil.Total);
        Assert.True(funil.ValorRecuperado >= 0);
        Assert.NotNull(funil.MesInicio);
        Assert.True(funil.MesInicio <= funil.MesFim);
    }

    [RequerInfraestrutura]
    public async Task ObterFunilDaClinicaAsync_DeClinicaSemDados_DevolveFunilZeradoSemJanela()
    {
        // Arrange
        await using var provedor = _infra.CriarProvedor();
        await using var escopo = provedor.CreateAsyncScope();
        var repositorio = escopo.ServiceProvider.GetRequiredService<ILeituraDoCoreRepository>();

        // Act
        var funil = await repositorio.ObterFunilDaClinicaAsync(ClinicaDeTeste);

        // Assert
        Assert.Equal(0, funil.Total);
        Assert.Equal(0m, funil.ValorPerdido);
        Assert.Null(funil.MesInicio);
        Assert.Null(funil.MesFim);
    }

    [RequerInfraestrutura]
    public async Task MetaIndicadorRepository_CicloCompleto_GravaLeAlteraERemoveSoNaClinicaDona()
    {
        // Arrange
        await using var provedor = _infra.CriarProvedor();
        var agora = DateTime.UtcNow;
        long id;

        try
        {
            // Act 1 — criar
            await using (var escopo = provedor.CreateAsyncScope())
            {
                var repositorio = escopo.ServiceProvider.GetRequiredService<IMetaIndicadorRepository>();
                var meta = MetaIndicador.Criar(ClinicaDeTeste, IndicadorMonitorado.TaxaCumprimento, 40m, agora);

                await repositorio.AdicionarAsync(meta);
                await repositorio.SalvarAsync();
                id = meta.Id;
            }

            // Act 2 — ler e alterar, num escopo novo: a leitura vem do banco
            await using (var escopo = provedor.CreateAsyncScope())
            {
                var repositorio = escopo.ServiceProvider.GetRequiredService<IMetaIndicadorRepository>();

                Assert.True(await repositorio.ExisteParaIndicadorAsync(ClinicaDeTeste, IndicadorMonitorado.TaxaCumprimento));
                Assert.False(await repositorio.ExisteParaIndicadorAsync(ClinicaDeTeste, IndicadorMonitorado.DeltaPontosPercentuais));
                Assert.Null(await repositorio.ObterDaClinicaAsync(ClinicaDeTeste + 1, id));

                var lida = await repositorio.ObterDaClinicaAsync(ClinicaDeTeste, id);
                Assert.NotNull(lida);
                lida.AlterarLimiar(45m, agora.AddMinutes(1));
                await repositorio.SalvarAsync();
            }

            // Act 3 — listar e remover
            await using (var escopo = provedor.CreateAsyncScope())
            {
                var repositorio = escopo.ServiceProvider.GetRequiredService<IMetaIndicadorRepository>();

                var metas = await repositorio.ListarDaClinicaAsync(ClinicaDeTeste);
                var meta = Assert.Single(metas);
                Assert.Equal(45m, meta.LimiarPercentual);
                Assert.Empty(await repositorio.ListarDaClinicaAsync(ClinicaDeTeste + 1));

                await repositorio.RemoverAsync(meta);
                await repositorio.SalvarAsync();
            }

            // Assert
            await using (var escopo = provedor.CreateAsyncScope())
            {
                var repositorio = escopo.ServiceProvider.GetRequiredService<IMetaIndicadorRepository>();
                Assert.Null(await repositorio.ObterDaClinicaAsync(ClinicaDeTeste, id));
            }
        }
        finally
        {
            await using var limpeza = _infra.CriarContexto();
            await limpeza.Metas.Where(m => m.IdClinica == ClinicaDeTeste).ExecuteDeleteAsync();
        }
    }

    [RequerInfraestrutura]
    public async Task SondasDeProntidao_ComOracleEMongoNoAr_FicamHealthy()
    {
        // Arrange
        await using var provedor = _infra.CriarProvedor();
        var saude = provedor.GetRequiredService<HealthCheckService>();

        // Act
        var relatorio = await saude.CheckHealthAsync(r => r.Tags.Contains("db"));

        // Assert
        Assert.Equal(HealthStatus.Healthy, relatorio.Entries["oracle"].Status);
        Assert.Equal(HealthStatus.Healthy, relatorio.Entries["mongo"].Status);
        Assert.Contains("VW_CLV_PAINEL_COORTE", relatorio.Entries["oracle"].Description);
    }

    private async Task<long> ClinicaComCoorteAsync()
    {
        await using var contexto = _infra.CriarContexto();

        return await contexto.PainelCoorte
            .GroupBy(v => v.IdClinica)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .FirstAsync();
    }
}
