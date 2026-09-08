using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Application.Obrigacoes;
using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Infrastructure.Persistencia.Leitura;
using Microsoft.EntityFrameworkCore;

namespace Clyvo.Insights.Infrastructure.Persistencia.Repositorios;

internal sealed class CoorteReadRepository : ICoorteReadRepository
{
    private const string GrupoTratado = "TRATADO";
    private const string GrupoControle = "CONTROLE";

    private readonly InsightsDbContext _contexto;

    public CoorteReadRepository(InsightsDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<IReadOnlyList<LinhaCoorte>> ObterCoortesDaClinicaAsync(
        long idClinica,
        CancellationToken cancellationToken = default)
    {
        var linhas = await _contexto.PainelCoorte
            .Where(v => v.IdClinica == idClinica)
            .ToListAsync(cancellationToken);

        return linhas.Select(Mapear).ToList();
    }

    /// <summary>
    /// Soma o funil sobre todos os meses e sobre os dois grupos.
    /// </summary>
    /// <remarks>
    /// A soma acontece no banco. Trazer as linhas mensais para somar em memória
    /// funcionaria com o volume de hoje e deixaria de funcionar sozinho quando
    /// a janela crescer.
    /// </remarks>
    public async Task<LinhaFunilObrigacoes> ObterFunilDaClinicaAsync(
        long idClinica,
        CancellationToken cancellationToken = default)
    {
        var linhas = _contexto.PainelReceita.Where(v => v.IdClinica == idClinica);

        var agregado = await linhas
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Sum(v => v.Total),
                Notificada = g.Sum(v => v.AlcancaramNotificada),
                Respondida = g.Sum(v => v.AlcancaramRespondida),
                Agendada = g.Sum(v => v.AlcancaramAgendada),
                Cumpridas = g.Sum(v => v.Cumpridas),
                Perdidas = g.Sum(v => v.Perdidas),
                Recuperado = g.Sum(v => v.ValorRecuperado),
                Perdido = g.Sum(v => v.ValorPerdido),
                MesInicio = g.Min(v => v.MesReferencia),
                MesFim = g.Max(v => v.MesReferencia)
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Clínica sem nenhuma obrigação vencida: funil zerado, não erro.
        return agregado is null
            ? new LinhaFunilObrigacoes(0, 0, 0, 0, 0, 0, 0m, 0m, null, null)
            : new LinhaFunilObrigacoes(
                agregado.Total,
                agregado.Notificada,
                agregado.Respondida,
                agregado.Agendada,
                agregado.Cumpridas,
                agregado.Perdidas,
                agregado.Recuperado,
                agregado.Perdido,
                agregado.MesInicio,
                agregado.MesFim);
    }

    private static LinhaCoorte Mapear(PainelCoorteView view) =>
        new(TraduzirGrupo(view.Grupo),
            view.ObrigacoesResolvidas,
            view.ObrigacoesCumpridas,
            view.PetsDistintos,
            view.TicketMedio);

    /// <summary>
    /// Traduz o rótulo da view para o enum do domínio. Rótulo desconhecido é
    /// falha do contrato de integração, e falha alto: silenciar num
    /// <c>default</c> jogaria as obrigações do grupo errado no denominador
    /// errado, e o número apareceria plausível na tela.
    /// </summary>
    private static GrupoCoorte TraduzirGrupo(string dsGrupo) => dsGrupo switch
    {
        GrupoTratado => GrupoCoorte.Tratado,
        GrupoControle => GrupoCoorte.Controle,
        _ => throw new InvalidOperationException(
            $"VW_CLV_PAINEL_COORTE devolveu o grupo '{dsGrupo}', que este serviço não conhece. " +
            $"Esperado '{GrupoTratado}' ou '{GrupoControle}'.")
    };
}
