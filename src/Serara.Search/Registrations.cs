using Microsoft.Extensions.DependencyInjection;

namespace Serara.Search;

public static class Registrations
{
    public static IServiceCollection AddSeraraSearch(this IServiceCollection services)
    {
        services.AddSingleton<ISearchBackendRegistry, SearchBackendRegistry>();
        services.AddScoped<ISearchExecutor, SearchExecutor>();
        services.AddScoped<ISearchRunService, SearchRunService>();
        services.AddOptions<SearchExecutionOptions>();
        return services;
    }
}
