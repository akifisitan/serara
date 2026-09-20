using Serara.Files;
using ZLogger.Formatters;

namespace Serara.ConsoleApp;

internal static class LoggingRegistrations
{
    public static IServiceCollection AddZLogFileLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        string basePath
    )
    {
        if (configuration.GetSection("Debug").GetValue<bool>("IsEnabled"))
        {
            services.AddDebugZLogFileLogging(configuration);

            return services;
        }

        services.AddLogging(x =>
        {
            x.ClearProviders()
                .AddConfiguration(configuration.GetSection("Logging"))
                .AddZLoggerFile(
                    Path.Combine(basePath, $"{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss}.log"),
                    options =>
                    {
                        options.UsePlainTextFormatter(ConfigureFormatter);
                    }
                );

            static void ConfigureFormatter(PlainTextZLoggerFormatter formatter)
            {
                formatter.SetPrefixFormatter(
                    $"[{0:local-longdate}][{1:short}][{2}]",
                    (in MessageTemplate template, in LogInfo logInfo) =>
                        template.Format(logInfo.Timestamp, logInfo.LogLevel, logInfo.Category)
                );
            }
        });

        return services;
    }

    private static IServiceCollection AddDebugZLogFileLogging(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var logFileName = Path.Combine("Logs", $"{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss}.log");

        services.AddLogging(x =>
        {
            x.ClearProviders()
                .AddConfiguration(configuration.GetSection("Logging"))
                .AddZLoggerFile(
                    logFileName,
                    options =>
                    {
                        options.CaptureThreadInfo = true;
                        options.UsePlainTextFormatter(ConfigureFormatter);
                    }
                );

            static void ConfigureFormatter(PlainTextZLoggerFormatter formatter)
            {
                formatter.SetPrefixFormatter(
                    $"[{0:local-longdate}][{1:short}][TID:{2:D2}]",
                    (in MessageTemplate template, in LogInfo logInfo) =>
                        template.Format(
                            logInfo.Timestamp,
                            logInfo.LogLevel,
                            logInfo.ThreadInfo.ThreadId
                        )
                );
            }
        });

        if (configuration.GetSection("DebugOptions").GetValue<bool>("LaunchLogFileOnStart"))
        {
            Utils.OpenWithFileEditor(logFileName);
        }

        return services;
    }
}
