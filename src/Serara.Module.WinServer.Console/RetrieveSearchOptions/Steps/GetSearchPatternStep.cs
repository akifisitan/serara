using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class GetSearchPatternStep
    : BuilderStateMachineStep<RetrieveSearchRunOptionsStateMachineRecord, Unit>
{
    private readonly WinServerConsoleInput _consoleInput;
    private readonly IMultiViewConsole _console;

    public GetSearchPatternStep(WinServerConsoleInput consoleInput, IMultiViewConsole console)
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
            .GetSearchPattern(cancellationToken)
            .ConfigureAwait(false);
        _console.Clear();

        if (result == PromptResult.Cancel)
        {
            return result;
        }

        input.SearchPattern = value;

        return result;
    }
}
