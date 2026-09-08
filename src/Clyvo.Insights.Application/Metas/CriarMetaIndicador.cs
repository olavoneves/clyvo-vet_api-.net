using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Excecoes;
using Clyvo.Insights.Application.Mapeamentos;
using Clyvo.Insights.Domain.Metas;

namespace Clyvo.Insights.Application.Metas;

/// <summary>Define uma meta para um indicador da clínica do tenant.</summary>
public sealed class CriarMetaIndicador
{
    private readonly IMetaIndicadorRepository _repositorio;
    private readonly ITenantContext _tenant;

    public CriarMetaIndicador(IMetaIndicadorRepository repositorio, ITenantContext tenant)
    {
        _repositorio = repositorio;
        _tenant = tenant;
    }

    /// <exception cref="ConflitoDeRecursoException">Já existe meta para esse indicador na clínica.</exception>
    public async Task<MetaIndicadorDto> ExecutarAsync(
        IndicadorMonitorado indicador,
        decimal limiarPercentual,
        CancellationToken cancellationToken = default)
    {
        var idClinica = _tenant.IdClinica;

        if (await _repositorio.ExisteParaIndicadorAsync(idClinica, indicador, cancellationToken))
        {
            throw new ConflitoDeRecursoException(
                $"A clínica já tem meta definida para o indicador {indicador}. " +
                "Cada indicador tem um piso só — altere o existente em vez de criar outro.");
        }

        var meta = MetaIndicador.Criar(idClinica, indicador, limiarPercentual, DateTime.UtcNow);

        await _repositorio.AdicionarAsync(meta, cancellationToken);
        await _repositorio.SalvarAsync(cancellationToken);

        return meta.ParaDto();
    }
}
