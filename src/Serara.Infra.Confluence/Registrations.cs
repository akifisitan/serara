using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Serara.Infra.Confluence;

public static class WinServerConfluenceRegistrations
{
    public static IServiceCollection AddConfluenceInfra(this IServiceCollection services)
    {
        services.TryAddSingleton<ConfluenceUserCredentialProvider>();
        services.TryAddSingleton<IConfluenceUserCredentialProvider>(sp =>
            sp.GetRequiredService<ConfluenceUserCredentialProvider>()
        );

        services.AddConfluenceHttpClient();

        return services;
    }

    private static IServiceCollection AddConfluenceHttpClient(this IServiceCollection services)
    {
        services
            .AddHttpClient(
                ConfluenceHttpClientFactory.ClientName,
                (sp, client) =>
                {
                    var options = sp.GetRequiredService<
                        IOptions<ConfluenceHttpClientFactoryOptions>
                    >().Value;

                    client.BaseAddress = new Uri(options.BaseUrl);
                    client.Timeout = TimeSpan.FromMilliseconds(options.TimeoutMs);
                }
            )
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<
                    IOptions<ConfluenceHttpClientFactoryOptions>
                >().Value;

                var handler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                };

                if (options.DisableSslValidation)
                {
                    handler.SslOptions = new()
                    {
                        RemoteCertificateValidationCallback = (_, _, _, _) => true,
                    };
                }

                return handler;
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(10));

        services.AddSingleton<IConfluenceHttpClientFactory, ConfluenceHttpClientFactory>();

        return services;
    }
}
