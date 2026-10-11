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
        using var applicationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken
        );
        var viewManagerTask = _viewManager.Start(applicationCancellation.Token);

        try
        {
            var mainViewTask = _mainView.Start(applicationCancellation.Token);
            var completedTask = await Task.WhenAny(mainViewTask, viewManagerTask)
                .ConfigureAwait(false);
            await completedTask.ConfigureAwait(false);
            await mainViewTask.ConfigureAwait(false);
        }
        finally
        {
            await applicationCancellation.CancelAsync().ConfigureAwait(false);

            try
            {
                await viewManagerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (applicationCancellation.IsCancellationRequested) { }
        }
    }
}
