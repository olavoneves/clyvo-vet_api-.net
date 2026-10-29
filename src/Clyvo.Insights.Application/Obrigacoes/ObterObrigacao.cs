using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Excecoes;
using Clyvo.Insights.Application.Mapeamentos;

namespace Clyvo.Insights.Application.Obrigacoes;

/// <summary>Uma obrigação da clínica do tenant.</summary>
public sealed class ObterObrigacao
{
    private readonly IDetalheObrigacaoRepository _repositorio;
    private readonly ITenantContext _tenant;

    public ObterObrigacao(IDetalheObrigacaoRepository repositorio, ITenantContext tenant)
    {
        _repositorio = repositorio;
        _tenant = tenant;
    }

    /// <exception cref="RecursoNaoEncontradoException">Obrigação inexistente, ou de outra clínica.</exception>
    public async Task<ObrigacaoDto> ExecutarAsync(long idObrigacao, CancellationToken cancellationToken = default)
    {
        var linha = await _repositorio.ObterDaClinicaAsync(_tenant.IdClinica, idObrigacao, cancellationToken)
            ?? throw new RecursoNaoEncontradoException($"Obrigação {idObrigacao} não encontrada.");

        return linha.ParaDto();
    }
}
