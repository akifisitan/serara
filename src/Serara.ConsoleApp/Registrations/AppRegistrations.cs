using Serara.Hosting;

namespace Serara.ConsoleApp;

internal static class AppRegistrations
{
    public static IServiceCollection AddSeraraConsoleApp(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSeraraHosting(configuration);

        services.AddZLogFileLogging(configuration, Path.Combine(AppContext.BaseDirectory, "Logs"));

        return services;
    }
}
