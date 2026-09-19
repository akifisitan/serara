using Serara.Tui;

namespace Serara.Console;

public sealed class SeraraConsoleApplication
{
    private readonly MainView _mainView;
    private readonly IViewManager _viewManager;

    internal SeraraConsoleApplication(MainView mainView, IViewManager viewManager)
    {
        _mainView = mainView;
        _viewManager = viewManager;
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        var viewManagerTask = _viewManager.Start(cancellationToken);

        await _mainView.Start(cancellationToken).ConfigureAwait(false);

        try
        {
            await viewManagerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }
}
