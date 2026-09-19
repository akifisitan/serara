using Serara.Core.Search;
using Serara.Search;
using Serara.Tui;
using Spectre.Console;

namespace Serara.Console;

internal interface ISearchRun
{
    Task<SearchRunResult> Execute(
        ISearchRequest searchOptions,
        string runId,
        CancellationToken cancellationToken
    );
}

internal sealed class SearchRun : ISearchRun
{
    private readonly ILogger<SearchRun> _logger;
    private readonly ISearchRunService _searchRunService;
    private readonly SearchResultsChannel _results;
    private readonly MultiViewConsole _console;

    public SearchRun(
        ILogger<SearchRun> logger,
        ISearchRunService searchRunService,
        SearchResultsChannel results,
        MultiViewConsole console
    )
    {
        _logger = logger;
        _searchRunService = searchRunService;
        _results = results;
        _console = console;
    }

    public async Task<SearchRunResult> Execute(
        ISearchRequest searchOptions,
        string runId,
        CancellationToken cancellationToken
    )
    {
        var resultLimitReached = 0;
        var outcome = await _searchRunService
            .Run(
                searchOptions,
                (result, _) =>
                {
                    if (
                        !_results.TryPublish(result)
                        && Interlocked.Exchange(ref resultLimitReached, 1) == 0
                    )
                    {
                        _console.WriteLine(
                            $"[{Colors.Yellow}]Result limit reached. Additional matching files will only be recorded in the logs.[/]"
                        );
                    }

                    _console.WriteLine(
                        $"[{Colors.Success}]Match found in: {result.Summary.EscapeMarkup()}[/]"
                    );
                    return ValueTask.CompletedTask;
                },
                progress =>
                    _console.WriteLine($"[{Colors.Log}]{progress.DisplayName.EscapeMarkup()}[/]"),
                cancellationToken
            )
            .ConfigureAwait(false);

        foreach (var failure in outcome.Failures)
        {
            _logger.ZLogError(
                failure.Exception,
                $"[{runId}] Search failed for {failure.WorkItem?.DisplayName}"
            );
        }

        return new SearchRunResult(outcome, Volatile.Read(ref resultLimitReached) != 0);
    }
}

internal sealed record SearchRunResult(SearchRunOutcome Outcome, bool ResultLimitReached);
