using Microsoft.Extensions.Hosting;
using Serara.Console;

namespace Serara.Hosting;

internal sealed class Startup : BackgroundService
{
    private readonly ConfigValidator _configValidator;
    private readonly SeraraConsoleApplication _application;

    public Startup(ConfigValidator configValidator, SeraraConsoleApplication application)
    {
        _configValidator = configValidator;
        _application = application;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configValidator.IsValid())
        {
            System.Console.WriteLine("Press any key to exit");
            System.Console.ReadKey(intercept: true);

            return;
        }

        await _application.Run(stoppingToken).ConfigureAwait(false);
    }
}
