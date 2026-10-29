using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Excecoes;
using Clyvo.Insights.Application.Mapeamentos;

namespace Clyvo.Insights.Application.Obrigacoes;

/// <summary>Lista, paginada, as obrigações da clínica do tenant.</summary>
public sealed class ListarObrigacoes
{
    private readonly IDetalheObrigacaoRepository _repositorio;
    private readonly ITenantContext _tenant;

    public ListarObrigacoes(IDetalheObrigacaoRepository repositorio, ITenantContext tenant)
    {
        _repositorio = repositorio;
        _tenant = tenant;
    }

    /// <exception cref="ConsultaInvalidaException">Algum parâmetro fora do que a listagem aceita.</exception>
    public async Task<ListagemObrigacoesDto> ExecutarAsync(
        ConsultaObrigacoesRequest consulta,
        CancellationToken cancellationToken = default)
    {
        // Validar antes de ler o tenant e de ir ao banco: consulta inválida não
        // custa uma ida ao Oracle.
        var criterios = CriteriosListagemObrigacoes.APartirDe(consulta);

        var pagina = await _repositorio.ListarDaClinicaAsync(_tenant.IdClinica, criterios, cancellationToken);

        return new ListagemObrigacoesDto(pagina.Mapear(ObrigacoesMapeamentos.ParaDto), criterios);
    }
}
