using Microsoft.Extensions.Hosting;
using Serara.Console;

namespace Serara.Hosting;

internal sealed class Startup : BackgroundService
{
    private readonly ConfigValidator _configValidator;
    private readonly SeraraConsoleApplication _application;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<Startup> _logger;
    private readonly ApplicationFailure _applicationFailure;

    public Startup(
        ConfigValidator configValidator,
        SeraraConsoleApplication application,
        IHostApplicationLifetime lifetime,
        ILogger<Startup> logger,
        ApplicationFailure applicationFailure
    )
    {
        _configValidator = configValidator;
        _application = application;
        _lifetime = lifetime;
        _logger = logger;
        _applicationFailure = applicationFailure;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!_configValidator.IsValid())
            {
                _applicationFailure.MarkFailed();
                return;
            }

            await _application.Run(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            _applicationFailure.MarkFailed(ex);
            _logger.ZLogCritical(ex, $"Serara stopped because of an unhandled error");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
