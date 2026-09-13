namespace Serara.Tui;

public interface IView
{
    Task Start(CancellationToken cancellationToken);
}
