using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class GetSearchTimesStep
    : BuilderStateMachineStep<RetrieveSearchRunOptionsStateMachineRecord, Unit>
{
    private readonly WinServerConsoleInput _consoleInput;
    private readonly IMultiViewConsole _console;

    public GetSearchTimesStep(WinServerConsoleInput consoleInput, IMultiViewConsole console)
    {
        _consoleInput = consoleInput;
        _console = console;
    }

    protected override async Task<PromptResultRecord<Unit>> ExecuteAsync(
        RetrieveSearchRunOptionsStateMachineRecord input,
        CancellationToken cancellationToken
    )
    {
        var (result, value) = await _consoleInput
            .GetSearchTimes(cancellationToken)
            .ConfigureAwait(false);
        _console.Clear();

        if (result == PromptResult.Cancel)
        {
            return result;
        }

        input.StartTime = value.StartTime;
        input.EndTime = value.EndTime;

        return result;
    }
}
