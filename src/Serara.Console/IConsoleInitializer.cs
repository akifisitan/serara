namespace Serara.Console;

public interface IConsoleInitializer
{
    Task Initialize(CancellationToken cancellationToken);
}
