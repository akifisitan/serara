using Microsoft.Extensions.DependencyInjection.Extensions;
using Serara.Core.Search;
using Serara.Files;
using Serara.Options;

namespace Serara.Module.WinServer;

public static class WinServerModuleRegistrations
{
    public static IServiceCollection AddWinServerModule(this IServiceCollection services)
    {
        services.AddSingleton<ISearchBackend, WinServerSearchBackend>();
        services.AddScoped<WinServerSearchRequestPreparation>();
        services.AddSingleton<WinApplicationMetadataMemoryCache>();
        services.AddScoped(sp => new WinApplicationMetadataProvider(
            sp.GetRequiredService<IWinApplicationMetadataLoader>(),
            sp.GetRequiredService<WinApplicationMetadataMemoryCache>()
        ));

        // Module options
        services
            .AddOptions<WinServerModuleOptions>()
            .BindConfiguration(nameof(WinServerModuleOptions))
            .WithValidator<WinServerModuleOptions, WinServerModuleOptionsValidator>();

        services
            .AddOptions<WinServerSearchOptions>()
            .BindConfiguration(nameof(WinServerSearchOptions))
            .WithValidator<WinServerSearchOptions, WinServerSearchOptionsValidator>();

        // File enumerator options
        services
            .AddOptions<FileEnumeratorOptions>()
            .BindConfiguration($"{nameof(FileEnumeratorOptions)}");

        // Core contracts
        services.TryAddSingleton<IWinApplicationPathProvider, WinApplicationPathProvider>();
        services.TryAddSingleton<IFileEnumerator, WinFileEnumerator>();
        return services;
    }
}
