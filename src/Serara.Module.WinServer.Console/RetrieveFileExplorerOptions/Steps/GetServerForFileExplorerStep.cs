using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class GetServerForFileExplorerStep
    : BuilderStateMachineStep<RetrieveFileExplorerOptionsStateMachineState, bool>
{
    private readonly WinServerConsoleInput _consoleInput;
    private readonly IWinApplicationPathProvider _pathProvider;

    public GetServerForFileExplorerStep(
        WinServerConsoleInput consoleInput,
        IWinApplicationPathProvider pathProvider
    )
    {
        _consoleInput = consoleInput;
        _pathProvider = pathProvider;
    }

    protected override async Task<PromptResultRecord<bool>> ExecuteAsync(
        RetrieveFileExplorerOptionsStateMachineState input,
        CancellationToken cancellationToken
    )
    {
        var (result, value) = await _consoleInput
            .GetServerForFileExplorer(input.SelectedData, cancellationToken)
            .ConfigureAwait(false);

        if (result == PromptResult.Cancel)
        {
            return result;
        }

        input.SelectedServer = value;
        input.HasArchiveServer = _pathProvider.SupportsArchive(input.SelectedData);

        return input.HasArchiveServer;
    }
}
