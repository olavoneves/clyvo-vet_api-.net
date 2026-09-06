using Clyvo.Insights.Application.Coortes;
using Microsoft.Extensions.DependencyInjection;

namespace Clyvo.Insights.Application;

/// <summary>Registra os casos de uso da camada de aplicação.</summary>
public static class InjecaoDeDependencia
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ObterAnaliseCoorte>();

        return services;
    }
}
