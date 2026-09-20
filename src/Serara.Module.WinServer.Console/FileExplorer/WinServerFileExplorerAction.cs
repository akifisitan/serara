using System.Diagnostics;
using Serara.Console;
using Serara.Files;
using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class WinServerFileExplorerAction : IInteractiveAction
{
    private readonly IMultiViewConsole _console;
    private readonly RetrieveFileExplorerOptionsStateMachine _stateMachine;
    private readonly ILogger<WinServerFileExplorerAction> _logger;

    public WinServerFileExplorerAction(
        IMultiViewConsole console,
        RetrieveFileExplorerOptionsStateMachine stateMachine,
        ILogger<WinServerFileExplorerAction> logger
    )
    {
        _console = console;
        _stateMachine = stateMachine;
        _logger = logger;
    }

    public string Id => "win-server-file-explorer";
    public string DisplayName => "File explorer";

    public async Task Execute(CancellationToken cancellationToken)
    {
        var handleEventsTask = HandleEvents(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _stateMachine.RunAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred while running file explorer");
            }
        }

        try
        {
            await handleEventsTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private async Task HandleEvents(CancellationToken cancellationToken)
    {
        var lastProcessedTs = -1L;
        const int debounceMs = 1000;

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await _console
                .ReadPromptEvent<OpenInExplorerEvent>(cancellationToken)
                .ConfigureAwait(false);

            if (
                lastProcessedTs != -1
                && Stopwatch.GetElapsedTime(lastProcessedTs, result.InsertedAt).TotalMilliseconds
                    < debounceMs
            )
            {
                continue;
            }

            lastProcessedTs = result.InsertedAt;

            Utils.OpenInFileExplorer(result.Event.Path);
        }
    }
}
