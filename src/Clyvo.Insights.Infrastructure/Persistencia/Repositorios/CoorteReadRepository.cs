using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Coortes;
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
