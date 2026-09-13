using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Serara.Tui;

public static class Registrations
{
    public static IServiceCollection AddSeraraTui(this IServiceCollection services)
    {
        services.TryAddSingleton<ViewManager>();
        services.TryAddSingleton<IViewManager>(sp => sp.GetRequiredService<ViewManager>());

        return services;
    }
}
