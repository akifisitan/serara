using Microsoft.Extensions.DependencyInjection;
using Serara.Options;

namespace Serara.Search;

public static class Registrations
{
    public static IServiceCollection AddSeraraSearch(this IServiceCollection services)
    {
        services.AddSingleton<ISearchBackendRegistry, SearchBackendRegistry>();
        services.AddScoped<ISearchExecutor, SearchExecutor>();
        services.AddScoped<ISearchRunService, SearchRunService>();
        services
            .AddOptions<SearchExecutionOptions>()
            .WithValidator<SearchExecutionOptions, SearchExecutionOptionsValidator>();

        return services;
    }
}
