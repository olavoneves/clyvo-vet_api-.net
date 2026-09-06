using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Application.Obrigacoes;
using Microsoft.Extensions.DependencyInjection;

namespace Clyvo.Insights.Application;

/// <summary>Registra os casos de uso da camada de aplicação.</summary>
public static class InjecaoDeDependencia
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ObterAnaliseCoorte>();
        services.AddScoped<ObterResumoObrigacoes>();

        services.AddScoped<CriarMetaIndicador>();
        services.AddScoped<ListarMetasIndicador>();
        services.AddScoped<AtualizarMetaIndicador>();
        services.AddScoped<RemoverMetaIndicador>();

        return services;
    }
}
