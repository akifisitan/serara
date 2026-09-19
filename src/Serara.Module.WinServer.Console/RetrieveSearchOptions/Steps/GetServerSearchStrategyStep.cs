using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class GetServerSearchStrategyStep
    : BuilderStateMachineStep<RetrieveSearchRunOptionsStateMachineRecord, Unit>
{
    private readonly WinServerConsoleInput _consoleInput;
    private readonly WinServerSearchRequestPreparation _requestPreparation;

    public GetServerSearchStrategyStep(
        WinServerConsoleInput consoleInput,
        WinServerSearchRequestPreparation requestPreparation
    )
    {
        _consoleInput = consoleInput;
        _requestPreparation = requestPreparation;
    }

    protected override async Task<PromptResultRecord<Unit>> ExecuteAsync(
        RetrieveSearchRunOptionsStateMachineRecord input,
        CancellationToken cancellationToken
    )
    {
        var (result, value) = await _consoleInput
            .GetServerSearchStrategy(
                allowArchiveSearch: _requestPreparation.CanSearchArchive(input.SelectedData),
                cancellationToken
            )
            .ConfigureAwait(false);

        if (result == PromptResult.Cancel)
        {
            return result;
        }

        input.SearchStrategy = value;

        return result;
    }
}
