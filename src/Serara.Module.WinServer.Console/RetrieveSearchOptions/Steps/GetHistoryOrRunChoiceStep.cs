using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class GetHistoryOrRunChoiceStep
    : BuilderStateMachineStep<RetrieveSearchRunOptionsStateMachineRecord, SearchOptionChoice>
{
    private readonly WinServerConsoleInput _consoleInput;
    private readonly SearchHistory? _searchHistory;
    private readonly ILogger<GetHistoryOrRunChoiceStep> _logger;

    public GetHistoryOrRunChoiceStep(
        WinServerConsoleInput consoleInput,
        SearchHistory? searchHistory,
        ILogger<GetHistoryOrRunChoiceStep> logger
    )
    {
        _consoleInput = consoleInput;
        _searchHistory = searchHistory;
        _logger = logger;
    }

    protected override async Task<PromptResultRecord<SearchOptionChoice>> ExecuteAsync(
        RetrieveSearchRunOptionsStateMachineRecord input,
        CancellationToken cancellationToken
    )
    {
        if (_searchHistory is null)
        {
            input.IsOnSearchHistoryBranch = false;
            return new(PromptResult.Success, SearchOptionChoice.NewRun);
        }

        try
        {
            await _searchHistory.EnsureInitialized(cancellationToken).ConfigureAwait(false);
            if (_searchHistory.RetrieveAll().Count == 0)
            {
                input.IsOnSearchHistoryBranch = false;
                return new(PromptResult.Success, SearchOptionChoice.NewRun);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"Failed to initialize search history");
            input.IsOnSearchHistoryBranch = false;
            return new(PromptResult.Success, SearchOptionChoice.NewRun);
        }

        var (result, value) = await _consoleInput
            .GetHistoryOrRunChoice(cancellationToken)
            .ConfigureAwait(false);

        if (result != PromptResult.Success)
        {
            throw new InvalidOperationException();
        }

        input.IsOnSearchHistoryBranch = value == SearchOptionChoice.FromHistory;

        return value;
    }
}
