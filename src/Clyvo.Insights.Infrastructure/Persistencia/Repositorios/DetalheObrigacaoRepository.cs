using System.Linq.Expressions;
using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Obrigacoes;
using Clyvo.Insights.Application.Paginacao;
using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Domain.Obrigacoes;
using Clyvo.Insights.Infrastructure.Persistencia.Leitura;
using Microsoft.EntityFrameworkCore;

namespace Clyvo.Insights.Infrastructure.Persistencia.Repositorios;

internal sealed class DetalheObrigacaoRepository : IDetalheObrigacaoRepository
{
    private const string FlagControle = "S";
    private const string FlagTratado = "N";

    /// <summary>
    /// Posição de cada status no funil, para ordenar por status sem cair na
    /// ordem alfabética do código. Vira um CASE no SQL.
    /// </summary>
    /// <remarks>
    /// Os literais repetem <see cref="StatusObrigacao"/> porque a expressão é
    /// traduzida para SQL, e o provider não traduz <c>Enum.ToString()</c>.
    /// </remarks>
    private static readonly Expression<Func<ObrigacaoDetalheView, int>> PosicaoNoFunil = v =>
        v.Status == "PREVISTA" ? (int)StatusObrigacao.Prevista
        : v.Status == "NOTIFICADA" ? (int)StatusObrigacao.Notificada
        : v.Status == "RESPONDIDA" ? (int)StatusObrigacao.Respondida
        : v.Status == "AGENDADA" ? (int)StatusObrigacao.Agendada
        : v.Status == "CUMPRIDA" ? (int)StatusObrigacao.Cumprida
        : v.Status == "PERDIDA" ? (int)StatusObrigacao.Perdida
        : v.Status == "CANCELADA" ? (int)StatusObrigacao.Cancelada
        : int.MaxValue;

    private readonly InsightsDbContext _contexto;

    public DetalheObrigacaoRepository(InsightsDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <summary>
    /// Filtra, conta, ordena e corta a página no banco.
    /// </summary>
    /// <remarks>
    /// Duas idas ao Oracle — <c>COUNT</c> e a página — sobre o mesmo
    /// <c>IQueryable</c> filtrado, que é o que garante que o total descreve o
    /// mesmo conjunto dos itens. Trazer as linhas para paginar em memória
    /// funcionaria com o seed e deixaria de funcionar sozinho em produção.
    /// </remarks>
    public async Task<Pagina<LinhaObrigacao>> ListarDaClinicaAsync(
        long idClinica,
        CriteriosListagemObrigacoes criterios,
        CancellationToken cancellationToken = default)
    {
        var filtrada = Filtrar(_contexto.ObrigacoesDetalhe.Where(v => v.IdClinica == idClinica), criterios);

        var total = await filtrada.CountAsync(cancellationToken);

        // Página além da última: não há o que buscar, e o total já está na mão.
        var linhas = criterios.Deslocamento >= total
            ? new List<ObrigacaoDetalheView>()
            : await Ordenar(filtrada, criterios)
                .Skip(criterios.Deslocamento)
                .Take(criterios.Tamanho)
                .ToListAsync(cancellationToken);

        return new Pagina<LinhaObrigacao>(
            linhas.Select(Mapear).ToList(), criterios.Pagina, criterios.Tamanho, total);
    }

    public async Task<LinhaObrigacao?> ObterDaClinicaAsync(
        long idClinica,
        long idObrigacao,
        CancellationToken cancellationToken = default)
    {
        var linha = await _contexto.ObrigacoesDetalhe
            .Where(v => v.IdClinica == idClinica && v.IdObrigacao == idObrigacao)
            .FirstOrDefaultAsync(cancellationToken);

        return linha is null ? null : Mapear(linha);
    }

    private static IQueryable<ObrigacaoDetalheView> Filtrar(
        IQueryable<ObrigacaoDetalheView> consulta,
        CriteriosListagemObrigacoes criterios)
    {
        if (criterios.Status is { } status)
        {
            var codigo = status.Codigo();
            consulta = consulta.Where(v => v.Status == codigo);
        }

        if (criterios.Grupo is { } grupo)
        {
            var flag = grupo == GrupoCoorte.Controle ? FlagControle : FlagTratado;
            consulta = consulta.Where(v => v.GrupoControle == flag);
        }

        if (criterios.Categoria is { } categoria)
        {
            consulta = consulta.Where(v => v.Categoria == categoria);
        }

        if (criterios.De is { } de)
        {
            var inicio = de.ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(v => v.DataPrevista >= inicio);
        }

        if (criterios.Ate is { } ate)
        {
            // Até o fim do dia, inclusive: DATE pode carregar hora, e
            // "<= ate 00:00" deixaria de fora o que vence no próprio dia.
            var limite = ate.AddDays(1).ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(v => v.DataPrevista < limite);
        }

        if (criterios.IdPet is { } idPet)
        {
            consulta = consulta.Where(v => v.IdPet == idPet);
        }

        return consulta;
    }

    /// <summary>
    /// Ordena pelo campo pedido e desempata por <c>ID_OBRIGACAO</c> crescente.
    /// </summary>
    /// <remarks>
    /// Sem o desempate, duas obrigações com a mesma data podem trocar de
    /// posição entre uma consulta e outra, e a mesma obrigação aparece em duas
    /// páginas enquanto outra não aparece em nenhuma. O Oracle não promete
    /// ordem estável para chaves empatadas.
    /// </remarks>
    private static IQueryable<ObrigacaoDetalheView> Ordenar(
        IQueryable<ObrigacaoDetalheView> consulta,
        CriteriosListagemObrigacoes criterios)
    {
        var ordenada = criterios.OrdenarPor switch
        {
            CampoOrdenacaoObrigacoes.Status => OrdenarPor(consulta, PosicaoNoFunil, criterios.Direcao),
            CampoOrdenacaoObrigacoes.Pet => OrdenarPor(consulta, v => v.NomePet, criterios.Direcao),
            CampoOrdenacaoObrigacoes.ValorEstimado => OrdenarPor(consulta, v => v.ValorEstimado, criterios.Direcao),
            _ => OrdenarPor(consulta, v => v.DataPrevista, criterios.Direcao)
        };

        return ordenada.ThenBy(v => v.IdObrigacao);
    }

    private static IOrderedQueryable<ObrigacaoDetalheView> OrdenarPor<TChave>(
        IQueryable<ObrigacaoDetalheView> consulta,
        Expression<Func<ObrigacaoDetalheView, TChave>> chave,
        DirecaoOrdenacao direcao) =>
        direcao == DirecaoOrdenacao.Desc ? consulta.OrderByDescending(chave) : consulta.OrderBy(chave);

    private static LinhaObrigacao Mapear(ObrigacaoDetalheView view) =>
        new(view.IdObrigacao,
            view.IdPet,
            view.NomePet,
            view.NomeTutor,
            view.Protocolo,
            view.Categoria,
            view.Etapa,
            TraduzirStatus(view),
            TraduzirGrupo(view),
            view.DataPrevista,
            view.JanelaInicio,
            view.JanelaFim,
            view.ValorEstimado,
            view.ValorRealizado);

    /// <summary>
    /// Status desconhecido é falha do contrato de integração, e falha alto —
    /// como o grupo desconhecido na leitura de coorte.
    /// </summary>
    private static StatusObrigacao TraduzirStatus(ObrigacaoDetalheView view) =>
        CodigoDeStatus.TentarInterpretar(view.Status, out var status)
            ? status
            : throw new InvalidOperationException(
                $"VW_CLV_OBRIGACAO_DETALHE devolveu o status '{view.Status}' na obrigação " +
                $"{view.IdObrigacao}, que este serviço não conhece.");

    private static GrupoCoorte TraduzirGrupo(ObrigacaoDetalheView view) => view.GrupoControle switch
    {
        FlagControle => GrupoCoorte.Controle,
        FlagTratado => GrupoCoorte.Tratado,
        _ => throw new InvalidOperationException(
            $"VW_CLV_OBRIGACAO_DETALHE devolveu FL_GRUPO_CONTROLE '{view.GrupoControle}' na obrigação " +
            $"{view.IdObrigacao}. Esperado '{FlagControle}' ou '{FlagTratado}'.")
    };
}
