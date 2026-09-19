using Serara.Console;
using Serara.Tui;
using Spectre.Console;

namespace Serara.Module.WinServer.Console;

internal sealed class AcceptAndStartStep
    : BuilderStateMachineStep<RetrieveSearchRunOptionsStateMachineRecord, SearchRequestResolution>
{
    private readonly IConsoleInput _consoleInput;
    private readonly MultiViewConsole _console;
    private readonly WinServerSearchRequestPreparation _requestPreparation;

    public AcceptAndStartStep(
        IConsoleInput consoleInput,
        MultiViewConsole console,
        WinServerSearchRequestPreparation requestPreparation
    )
    {
        _consoleInput = consoleInput;
        _console = console;
        _requestPreparation = requestPreparation;
    }

    protected override async Task<PromptResultRecord<SearchRequestResolution>> ExecuteAsync(
        RetrieveSearchRunOptionsStateMachineRecord input,
        CancellationToken cancellationToken
    )
    {
        var preparation = _requestPreparation.Prepare(
            input.SelectedData,
            input.SelectedServers,
            input.StartTime,
            input.EndTime,
            input.SearchPattern,
            input.SearchStrategy
        );
        if (!preparation.IsValid)
        {
            foreach (var error in preparation.Errors)
            {
                _console.WriteLine($"[{Colors.Error}]{error.Message.EscapeMarkup()}[/]");
            }

            return new(PromptResult.Success, GetResolution(preparation.Errors[0].Field));
        }

        var request = preparation.Request!;

        _console.WriteLine(
            $"""
            [{Colors.Title}]Running query with the following parameters:[/]
            {request.ToConsoleDisplay()}
            """
        );

        var (result, value) = await _consoleInput
            .GetUserConfirmation(
                "Confirm and start search?",
                withCancel: true,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        _console.Clear();

        if (result == PromptResult.Cancel || !value)
        {
            return new(
                PromptResult.Cancel,
                input.IsOnSearchHistoryBranch
                    ? SearchRequestResolution.RejectedHistory
                    : SearchRequestResolution.RejectedNewRun
            );
        }

        input.Finish(request);

        return SearchRequestResolution.Complete;
    }

    private static SearchRequestResolution GetResolution(WinServerSearchRequestField field) =>
        field switch
        {
            WinServerSearchRequestField.Application => SearchRequestResolution.InvalidApplication,
            WinServerSearchRequestField.Servers => SearchRequestResolution.InvalidServers,
            WinServerSearchRequestField.SearchStrategy =>
                SearchRequestResolution.InvalidSearchStrategy,
            WinServerSearchRequestField.TimeRange => SearchRequestResolution.InvalidTimeRange,
            WinServerSearchRequestField.SearchPattern =>
                SearchRequestResolution.InvalidSearchPattern,
            _ => throw new NotSupportedException(),
        };
}
