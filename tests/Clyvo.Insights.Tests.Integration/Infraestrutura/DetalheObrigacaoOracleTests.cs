using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Obrigacoes;
using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Domain.Obrigacoes;
using Clyvo.Insights.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Clyvo.Insights.Tests.Integration.Infraestrutura;

/// <summary>
/// A listagem de obrigações contra a <c>VW_CLV_OBRIGACAO_DETALHE</c> de verdade.
/// </summary>
/// <remarks>
/// O dublê em memória prova a fatia HTTP; o que só o Oracle prova é o SQL — que
/// o filtro, o <c>OFFSET/FETCH</c>, o CASE da ordenação por status e o
/// <c>COUNT</c> traduzem e descrevem o mesmo conjunto. Como nos outros testes
/// deste grupo, nada de contagem fixa: o total esperado sai de uma consulta
/// direta à view, feita no próprio teste.
/// </remarks>
[Collection(ColecaoDeInfraestruturaReal.Nome)]
public class DetalheObrigacaoOracleTests
{
    private readonly InfraestruturaRealFixture _infra;

    public DetalheObrigacaoOracleTests(InfraestruturaRealFixture infra)
    {
        _infra = infra;
    }

    private static CriteriosListagemObrigacoes Criterios(ConsultaObrigacoesRequest consulta) =>
        CriteriosListagemObrigacoes.APartirDe(consulta);

    [RequerInfraestrutura]
    public async Task ListarDaClinicaAsync_NaViewReal_PaginaSemSobreporEContaOTotalDaClinica()
    {
        // Arrange
        await using var contexto = _infra.CriarContexto();
        var idClinica = await ClinicaComMaisObrigacoesAsync(contexto);
        var totalNaView = await contexto.ObrigacoesDetalhe.CountAsync(v => v.IdClinica == idClinica);

        await using var provedor = _infra.CriarProvedor();
        await using var escopo = provedor.CreateAsyncScope();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IDetalheObrigacaoRepository>();

        var primeira = Criterios(new ConsultaObrigacoesRequest { Tamanho = 5 });

        // Act
        var pagina1 = await repositorio.ListarDaClinicaAsync(idClinica, primeira);
        var pagina2 = await repositorio.ListarDaClinicaAsync(idClinica, primeira.NaPagina(2));

        // Assert
        Assert.True(totalNaView > 10, $"A clínica {idClinica} precisa de mais de duas páginas de 5 para o teste valer.");
        Assert.Equal(totalNaView, pagina1.TotalItens);
        Assert.Equal(totalNaView, pagina2.TotalItens);
        Assert.Equal(5, pagina1.Itens.Count);
        Assert.Equal(5, pagina2.Itens.Count);
        Assert.Empty(pagina1.Itens.Select(i => i.IdObrigacao).Intersect(pagina2.Itens.Select(i => i.IdObrigacao)));

        // Ordenação padrão (data prevista, desempate por id) contínua entre as páginas
        var emSequencia = pagina1.Itens.Concat(pagina2.Itens).Select(i => (i.DataPrevista, i.IdObrigacao)).ToList();
        Assert.Equal(emSequencia.OrderBy(x => x.DataPrevista).ThenBy(x => x.IdObrigacao), emSequencia);
    }

    [RequerInfraestrutura]
    public async Task ListarDaClinicaAsync_ComFiltrosCombinados_BateComAConsultaDiretaAView()
    {
        // Arrange
        await using var contexto = _infra.CriarContexto();
        var idClinica = await ClinicaComMaisObrigacoesAsync(contexto);
        var esperado = await contexto.ObrigacoesDetalhe.CountAsync(v =>
            v.IdClinica == idClinica && v.Status == "CUMPRIDA" && v.GrupoControle == "N");

        await using var provedor = _infra.CriarProvedor();
        await using var escopo = provedor.CreateAsyncScope();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IDetalheObrigacaoRepository>();

        // Act
        var pagina = await repositorio.ListarDaClinicaAsync(idClinica, Criterios(new ConsultaObrigacoesRequest
        {
            Status = "cumprida",
            Grupo = "tratamento",
            OrdenarPor = "valorEstimado",
            Direcao = "desc",
            Tamanho = 100
        }));

        // Assert
        Assert.Equal(esperado, pagina.TotalItens);
        Assert.All(pagina.Itens, i =>
        {
            Assert.Equal(StatusObrigacao.Cumprida, i.Status);
            Assert.Equal(GrupoCoorte.Tratado, i.Grupo);
        });

        var valores = pagina.Itens.Where(i => i.ValorEstimado is not null).Select(i => i.ValorEstimado!.Value).ToList();
        Assert.Equal(valores.OrderByDescending(v => v), valores);
    }

    [RequerInfraestrutura]
    public async Task ListarDaClinicaAsync_OrdenadoPorStatus_SegueAOrdemDoFunil()
    {
        // Arrange
        await using var contexto = _infra.CriarContexto();
        var idClinica = await ClinicaComMaisObrigacoesAsync(contexto);

        await using var provedor = _infra.CriarProvedor();
        await using var escopo = provedor.CreateAsyncScope();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IDetalheObrigacaoRepository>();

        // Act
        var pagina = await repositorio.ListarDaClinicaAsync(
            idClinica, Criterios(new ConsultaObrigacoesRequest { OrdenarPor = "status", Tamanho = 100 }));

        // Assert — o CASE traduzido no SQL ordena pela posição no funil
        var posicoes = pagina.Itens.Select(i => (int)i.Status).ToList();
        Assert.Equal(posicoes.Order(), posicoes);
    }

    [RequerInfraestrutura]
    public async Task ObterDaClinicaAsync_ComObrigacaoDeOutraClinica_DevolveNulo()
    {
        // Arrange — uma obrigação real, procurada com o id de outra clínica
        await using var contexto = _infra.CriarContexto();
        var obrigacao = await contexto.ObrigacoesDetalhe.OrderBy(v => v.IdObrigacao).FirstAsync();

        await using var provedor = _infra.CriarProvedor();
        await using var escopo = provedor.CreateAsyncScope();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IDetalheObrigacaoRepository>();

        // Act
        var daPropria = await repositorio.ObterDaClinicaAsync(obrigacao.IdClinica, obrigacao.IdObrigacao);
        var deOutra = await repositorio.ObterDaClinicaAsync(obrigacao.IdClinica + 999_000, obrigacao.IdObrigacao);

        // Assert
        Assert.NotNull(daPropria);
        Assert.Equal(obrigacao.IdObrigacao, daPropria!.IdObrigacao);
        Assert.Null(deOutra);
    }

    private static Task<long> ClinicaComMaisObrigacoesAsync(InsightsDbContext contexto) =>
        contexto.ObrigacoesDetalhe
            .GroupBy(v => v.IdClinica)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstAsync();
}
