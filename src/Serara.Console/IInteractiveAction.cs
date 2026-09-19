namespace Serara.Console;

public interface IInteractiveAction
{
    string Id { get; }
    string DisplayName { get; }
    Task Execute(CancellationToken cancellationToken);
}
