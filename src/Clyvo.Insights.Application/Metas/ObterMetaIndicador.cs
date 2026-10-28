using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Excecoes;
using Clyvo.Insights.Application.Mapeamentos;

namespace Clyvo.Insights.Application.Metas;

/// <summary>Uma meta da clínica do tenant.</summary>
public sealed class ObterMetaIndicador
{
    private readonly IMetaIndicadorRepository _repositorio;
    private readonly ITenantContext _tenant;

    public ObterMetaIndicador(IMetaIndicadorRepository repositorio, ITenantContext tenant)
    {
        _repositorio = repositorio;
        _tenant = tenant;
    }

    /// <exception cref="RecursoNaoEncontradoException">Meta inexistente, ou de outra clínica.</exception>
    public async Task<MetaIndicadorDto> ExecutarAsync(long idMeta, CancellationToken cancellationToken = default)
    {
        var meta = await _repositorio.ObterDaClinicaAsync(_tenant.IdClinica, idMeta, cancellationToken)
            ?? throw new RecursoNaoEncontradoException($"Meta {idMeta} não encontrada.");

        return meta.ParaDto();
    }
}
