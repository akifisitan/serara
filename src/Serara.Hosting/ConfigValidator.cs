using System.Diagnostics;
using Serara.Console;
using Serara.Core.Configuration;
using Serara.Files;
using Serara.Tui;
using Spectre.Console;

namespace Serara.Hosting;

internal sealed class ConfigValidator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConfigValidator> _logger;

    private const string appName = "Serara";

    public ConfigValidator(IServiceProvider serviceProvider, ILogger<ConfigValidator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public bool IsValid()
    {
        try
        {
            var processPath = Environment.ProcessPath;

            if (!string.IsNullOrWhiteSpace(processPath))
            {
                var version = FileVersionInfo.GetVersionInfo(processPath).ProductVersion;
                System.Console.Title = $"{appName} v{version}";
                _logger.ZLogInformation($" Launched {appName} v{version}");
            }
            else
            {
                System.Console.Title = appName;
                _logger.ZLogInformation($" Launched {appName}");
                _logger.ZLogWarning(
                    $"An error occurred while retrieving {nameof(Environment.ProcessPath)}, cannot display version"
                );
            }

            AnsiConsole.MarkupLine($"[{Colors.Title}]Validating config...[/]");
            _logger.ZLogInformation($" Validating config...");

            // Validate core app options
            var fileSearchOptions = _serviceProvider
                .GetRequiredService<IOptions<FileSearchOptions>>()
                .Value;
            var zipFileSearchOptions = _serviceProvider
                .GetRequiredService<IOptions<ZipFileSearchOptions>>()
                .Value;
            var seraraConsoleAppOptions = _serviceProvider
                .GetRequiredService<IOptions<SeraraConsoleAppOptions>>()
                .Value;
            _ = _serviceProvider.GetRequiredService<IOptions<KeyBindingOptions>>().Value;

            _logger.ZLogInformation(
                $"""
                 Running application with the following core options
                {fileSearchOptions}
                {zipFileSearchOptions}
                {seraraConsoleAppOptions}
                """
            );

            foreach (var validator in _serviceProvider.GetServices<IModuleConfigurationValidator>())
            {
                var errors = validator.Validate(_serviceProvider);
                if (errors.Count > 0)
                {
                    throw new OptionsValidationException(
                        validator.Name,
                        typeof(IModuleConfigurationValidator),
                        errors
                    );
                }
            }

            AnsiConsole.Clear();

            return true;
        }
        catch (OptionsValidationException ex)
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLineInterpolated(
                $"[{Colors.Error}]Application config has errors:{Environment.NewLine}{ex.Message}[/]"
            );
            _logger.ZLogCritical(ex, $"Application config has errors");
            return false;
        }
    }
}
