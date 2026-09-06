using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Excecoes;

namespace Clyvo.Insights.Application.Metas;

/// <summary>Apaga uma meta da clínica do tenant.</summary>
public sealed class RemoverMetaIndicador
{
    private readonly IMetaIndicadorRepository _repositorio;
    private readonly ITenantContext _tenant;

    public RemoverMetaIndicador(IMetaIndicadorRepository repositorio, ITenantContext tenant)
    {
        _repositorio = repositorio;
        _tenant = tenant;
    }

    /// <exception cref="RecursoNaoEncontradoException">Meta inexistente, ou de outra clínica.</exception>
    public async Task ExecutarAsync(long idMeta, CancellationToken cancellationToken = default)
    {
        var meta = await _repositorio.ObterDaClinicaAsync(_tenant.IdClinica, idMeta, cancellationToken)
            ?? throw new RecursoNaoEncontradoException($"Meta {idMeta} não encontrada.");

        await _repositorio.RemoverAsync(meta, cancellationToken);
        await _repositorio.SalvarAsync(cancellationToken);
    }
}
