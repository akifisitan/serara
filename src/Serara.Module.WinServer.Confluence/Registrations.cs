using FluentValidation;
using Serara.Files;
using Serara.Infra.Confluence;

namespace Serara.Module.WinServer;

public static class WinServerConfluenceRegistrations
{
    public static IServiceCollection AddWinServerConfluenceSource(this IServiceCollection services)
    {
        services
            .AddOptions<ConfluenceMetadataLoaderOptions>()
            .BindConfiguration($"ConfluenceOptions:{nameof(ConfluenceMetadataLoaderOptions)}");
        services
            .AddOptions<ConfluenceHttpClientFactoryOptions>()
            .BindConfiguration($"ConfluenceOptions:{nameof(ConfluenceHttpClientFactoryOptions)}");
        services.AddSingleton<
            IValidator<ConfluenceMetadataLoaderOptions>,
            ConfluenceMetadataLoaderOptionsValidator
        >();
        services.AddSingleton<
            IValidateOptions<ConfluenceMetadataLoaderOptions>,
            FluentValidationOptions<ConfluenceMetadataLoaderOptions>
        >();
        services.AddSingleton<
            IValidator<ConfluenceHttpClientFactoryOptions>,
            ConfluenceHttpClientFactoryOptionsValidator
        >();
        services.AddSingleton<
            IValidateOptions<ConfluenceHttpClientFactoryOptions>,
            FluentValidationOptions<ConfluenceHttpClientFactoryOptions>
        >();

        services.AddConfluenceInfra();
        services.AddSingleton<ConfluenceMetadataLoader>();
        services.AddSingleton<CachedMetadataLoader>();
        services.AddSingleton<JsonFileStore>();
        services.AddScoped<IWinApplicationMetadataLoader, WinServerMetadataLoader>();
        services.AddSingleton<LocalDebugMetadataLoader>();

        return services;
    }
}
