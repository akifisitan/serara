using Serara.Console;
using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class GetLiveOrArchiveChoiceForFileExplorerStep
    : BuilderStateMachineStep<RetrieveFileExplorerOptionsStateMachineState, Unit>
{
    private readonly IConsoleInput _consoleInput;

    public GetLiveOrArchiveChoiceForFileExplorerStep(IConsoleInput consoleInput)
    {
        _consoleInput = consoleInput;
    }

    protected override async Task<PromptResultRecord<Unit>> ExecuteAsync(
        RetrieveFileExplorerOptionsStateMachineState input,
        CancellationToken cancellationToken
    )
    {
        var (result, value) = await _consoleInput
            .GetUserConfirmation(
                $"[{Colors.Title}]Explore archive server?[/]",
                defaultValue: true,
                withNavigation: true,
                withCancel: true,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        if (result == PromptResult.Cancel)
        {
            return result;
        }

        input.IsArchive = value;

        return result;
    }
}
