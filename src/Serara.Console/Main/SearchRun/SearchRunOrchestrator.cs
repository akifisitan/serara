using Serara.Core.Search;
using Serara.Files;
using Serara.Tui;

namespace Serara.Console;

internal sealed class SearchRunOrchestrator
{
    private readonly ILogger<SearchRunOrchestrator> _logger;
    private readonly SearchRun _searchRun;
    private readonly ITimeProvider _timeProvider;
    private readonly IMultiViewConsole _console;
    private readonly SearchResultsChannel _resultsChannel;

    public SearchRunOrchestrator(
        ILogger<SearchRunOrchestrator> logger,
        SearchRun searchRun,
        ITimeProvider timeProvider,
        IMultiViewConsole console,
        SearchResultsChannel resultsChannel
    )
    {
        _logger = logger;
        _searchRun = searchRun;
        _timeProvider = timeProvider;
        _console = console;
        _resultsChannel = resultsChannel;
    }

    public async Task<SearchRunFinishedEvent> RunSearch(
        ISearchRequest searchOptions,
        CancellationToken cancellationToken
    )
    {
        using var ctrlCCancellationTokenSource = new CancellationTokenSource();
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            ctrlCCancellationTokenSource.Token
        );
        var cancelled = 0;
        var runId = _timeProvider.LocalNow().ToString("yyyyMMddHHmmssfff");

        _resultsChannel.Reset();

        SearchRunResult result;
        try
        {
            System.Console.CancelKeyPress += CancelQuery;
            result = await _searchRun
                .Execute(searchOptions, runId, linkedCancellationTokenSource.Token)
                .ConfigureAwait(false);
        }
        finally
        {
            System.Console.CancelKeyPress -= CancelQuery;
        }

        return new SearchRunFinishedEvent(
            result.Outcome.IsCancelled,
            result.Outcome.FailureCount,
            result.ResultLimitReached
        );

        void CancelQuery(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;

            if (Interlocked.Exchange(ref cancelled, 1) == 1)
            {
                return;
            }

            ctrlCCancellationTokenSource.Cancel();

            _logger.ZLogInformation($"[{runId}] Query cancellation requested");

            _console.WriteLine($"[{Colors.Yellow}]Query cancellation requested[/]");
        }
    }
}
