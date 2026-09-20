using Serara.Console;
using Serara.Core.Configuration;
using Serara.History;
using Serara.Module.WinServer;
using Serara.Module.WinServer.Console;

namespace Serara.Hosting;

public static class HostingRegistrations
{
    public static IServiceCollection AddSeraraHosting(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSeraraConsole(configuration);
        services.AddWinServerModule();
        services.AddWinServerConsole();
        services.AddWinServerConfluenceSource();
        services.AddSingleton<IModuleConfigurationValidator, HostingCompositionValidator>();

        var historyConfiguration = configuration.GetSection(nameof(SeraraHistoryOptions));

        if (historyConfiguration.GetValue(nameof(SeraraHistoryOptions.Enabled), true))
        {
            services.AddSeraraHistory(options => historyConfiguration.Bind(options));
        }

        services.AddHostedService<Startup>();
        services.AddSingleton<ConfigValidator>();

        return services;
    }
}
