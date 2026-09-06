using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Mapeamentos;

namespace Clyvo.Insights.Application.Metas;

/// <summary>Lista as metas da clínica do tenant.</summary>
public sealed class ListarMetasIndicador
{
    private readonly IMetaIndicadorRepository _repositorio;
    private readonly ITenantContext _tenant;

    public ListarMetasIndicador(IMetaIndicadorRepository repositorio, ITenantContext tenant)
    {
        _repositorio = repositorio;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<MetaIndicadorDto>> ExecutarAsync(
        CancellationToken cancellationToken = default)
    {
        var metas = await _repositorio.ListarDaClinicaAsync(_tenant.IdClinica, cancellationToken);

        return metas.ParaDto();
    }
}
