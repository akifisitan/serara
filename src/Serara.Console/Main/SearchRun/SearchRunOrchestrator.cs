using Serara.Core.Search;
using Serara.Files;
using Serara.Tui;

namespace Serara.Console;

internal sealed class SearchRunOrchestrator
{
    private readonly ILogger<SearchRunOrchestrator> _logger;
    private readonly ISearchRun _searchRun;
    private readonly ITimeProvider _timeProvider;
    private readonly IMultiViewConsole _console;
    private readonly SearchResultsChannel _resultsChannel;
    private readonly IViewManager _viewManager;
    private readonly KeyRegistrations _keyRegistrations;

    public SearchRunOrchestrator(
        ILogger<SearchRunOrchestrator> logger,
        ISearchRun searchRun,
        ITimeProvider timeProvider,
        IMultiViewConsole console,
        SearchResultsChannel resultsChannel,
        IViewManager viewManager,
        KeyRegistrations keyRegistrations
    )
    {
        _logger = logger;
        _searchRun = searchRun;
        _timeProvider = timeProvider;
        _console = console;
        _resultsChannel = resultsChannel;
        _viewManager = viewManager;
        _keyRegistrations = keyRegistrations;
    }

    public async Task<SearchRunFinishedEvent> RunSearch(
        ISearchRequest searchOptions,
        CancellationToken cancellationToken
    )
    {
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken
        );
        var cancelled = 0;
        var runId = _timeProvider.LocalNow().ToString("yyyyMMddHHmmssfff");

        _resultsChannel.Reset();

        using var cancelRegistration = _viewManager.RegisterKeyHandler(
            SearchRunView.Id,
            _keyRegistrations.CancelPress,
            CancelQuery
        );
        var result = await _searchRun
            .Execute(searchOptions, runId, linkedCancellationTokenSource.Token)
            .ConfigureAwait(false);

        return new SearchRunFinishedEvent(
            result.Outcome.CompletionMode,
            result.Outcome.FailureCount,
            result.ResultLimitReached
        );

        void CancelQuery()
        {
            if (Interlocked.Exchange(ref cancelled, 1) == 1)
            {
                return;
            }

            linkedCancellationTokenSource.Cancel();

            _logger.ZLogInformation($"[{runId}] Query cancellation requested");

            _console.WriteLine($"[{Colors.Yellow}]Query cancellation requested[/]");
        }
    }
}
