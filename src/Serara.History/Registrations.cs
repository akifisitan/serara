using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Serara.History;

public static class Registrations
{
    public static IServiceCollection AddSeraraHistory(
        this IServiceCollection services,
        Action<SeraraHistoryOptions>? configure = null
    )
    {
        services.AddOptions<SeraraHistoryOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<SearchHistoryStore>();
        services.TryAddSingleton<ISearchHistoryStore>(sp =>
            sp.GetRequiredService<SearchHistoryStore>()
        );

        return services;
    }
}
