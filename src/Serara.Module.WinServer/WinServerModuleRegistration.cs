using Serara.Core.Configuration;

namespace Serara.Module.WinServer;

public sealed class WinServerModuleRegistration : IModuleConfigurationValidator
{
    private readonly ILogger<WinServerModuleRegistration> _logger;

    public WinServerModuleRegistration(ILogger<WinServerModuleRegistration> logger)
    {
        _logger = logger;
    }

    public string Name => "WinServer";

    public IReadOnlyList<string> Validate(IServiceProvider serviceProvider)
    {
        List<string> errors = [];

        WinServerModuleOptions? winServerModuleOptions = null;
        try
        {
            winServerModuleOptions = serviceProvider
                .GetRequiredService<IOptions<WinServerModuleOptions>>()
                .Value;
        }
        catch (OptionsValidationException ex)
        {
            errors.AddRange(ex.Failures);
        }

        try
        {
            _ = serviceProvider.GetRequiredService<IOptions<WinServerSearchOptions>>().Value;
        }
        catch (OptionsValidationException ex)
        {
            errors.AddRange(ex.Failures);
        }

        if (errors.Count == 0)
        {
            _logger.ZLogInformation(
                $"""
                 [{Name}] Module options:
                {winServerModuleOptions}
                """
            );
        }

        return errors;
    }
}
