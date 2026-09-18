using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serara.Core.Configuration;
using Serara.Core.Search;
using Serara.Files;

namespace Serara.Module.WinServer;

public static class WinServerModuleRegistrations
{
    public static IServiceCollection AddWinServerModule(this IServiceCollection services)
    {
        services.TryAddSingleton<WinServerModuleRegistration>();
        services.AddSingleton<IModuleConfigurationValidator>(sp =>
            sp.GetRequiredService<WinServerModuleRegistration>()
        );
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
            .BindConfiguration(nameof(WinServerModuleOptions));

        services
            .AddSingleton<IValidator<WinServerModuleOptions>, WinServerModuleOptionsValidator>()
            .AddSingleton<
                IValidateOptions<WinServerModuleOptions>,
                FluentValidationOptions<WinServerModuleOptions>
            >();

        services
            .AddOptions<WinServerSearchOptions>()
            .BindConfiguration(nameof(WinServerSearchOptions));
        services
            .AddSingleton<IValidator<WinServerSearchOptions>, WinServerSearchOptionsValidator>()
            .AddSingleton<
                IValidateOptions<WinServerSearchOptions>,
                FluentValidationOptions<WinServerSearchOptions>
            >();

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
