using Serara.Infra.Confluence;
using Serara.Options;

namespace Serara.Module.WinServer;

public static class WinServerConfluenceRegistrations
{
    public static IServiceCollection AddWinServerConfluenceSource(this IServiceCollection services)
    {
        services
            .AddOptions<ConfluenceMetadataLoaderOptions>()
            .BindConfiguration($"ConfluenceOptions:{nameof(ConfluenceMetadataLoaderOptions)}")
            .WithValidator<
                ConfluenceMetadataLoaderOptions,
                ConfluenceMetadataLoaderOptionsValidator
            >();
        services
            .AddOptions<ConfluenceHttpClientFactoryOptions>()
            .BindConfiguration($"ConfluenceOptions:{nameof(ConfluenceHttpClientFactoryOptions)}");

        services.AddConfluenceInfra();
        services.AddSingleton<ConfluenceMetadataLoader>();
        services.AddSingleton<CachedMetadataLoader>();
        services.AddSingleton<JsonFileStore>();
        services.AddScoped<IWinApplicationMetadataLoader, WinServerMetadataLoader>();
        services.AddSingleton<LocalDebugMetadataLoader>();

        return services;
    }
}
