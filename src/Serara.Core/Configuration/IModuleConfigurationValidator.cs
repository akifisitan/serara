namespace Serara.Core.Configuration;

public interface IModuleConfigurationValidator
{
    string Name { get; }
    IReadOnlyList<string> Validate(IServiceProvider serviceProvider);
}
