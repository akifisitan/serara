using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class GetServersToSearchStep
    : BuilderStateMachineStep<RetrieveSearchRunOptionsStateMachineRecord, Unit>
{
    private readonly WinServerConsoleInput _consoleInput;

    public GetServersToSearchStep(WinServerConsoleInput consoleInput)
    {
        _consoleInput = consoleInput;
    }

    protected override async Task<PromptResultRecord<Unit>> ExecuteAsync(
        RetrieveSearchRunOptionsStateMachineRecord input,
        CancellationToken cancellationToken
    )
    {
        var (result, value) = await _consoleInput
            .GetServersToSearch(input.SelectedData, cancellationToken)
            .ConfigureAwait(false);

        if (result == PromptResult.Cancel)
        {
            return result;
        }

        input.SelectedServers = value;

        return result;
    }
}
