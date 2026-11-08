using System.Collections.Concurrent;
using Serara.Core.Search;

namespace Serara.Search;

public sealed record SearchRunFailure(
    Exception Exception,
    SearchWorkItem? WorkItem,
    string? DiscoveryPath = null
);

public sealed record SearchRunOutcome(
    SearchCompletionMode CompletionMode,
    IReadOnlyList<SearchRunFailure> Failures
)
{
    public int FailureCount => Failures.Count;
}

public interface ISearchRunService
{
    Task<SearchRunOutcome> Run(
        ISearchRequest request,
        Func<ModuleSearchResult, CancellationToken, ValueTask> publish,
        Action<ModuleSearchProgress> progress,
        CancellationToken cancellationToken
    );
}

public sealed class SearchRunService : ISearchRunService
{
    private readonly ISearchExecutor _searchExecutor;

    public SearchRunService(ISearchExecutor searchExecutor)
    {
        _searchExecutor = searchExecutor;
    }

    public async Task<SearchRunOutcome> Run(
        ISearchRequest request,
        Func<ModuleSearchResult, CancellationToken, ValueTask> publish,
        Action<ModuleSearchProgress> progress,
        CancellationToken cancellationToken
    )
    {
        var failures = new ConcurrentQueue<SearchRunFailure>();

        if (cancellationToken.IsCancellationRequested)
        {
            return new SearchRunOutcome(SearchCompletionMode.Cancelled, failures.ToArray());
        }

        try
        {
            var completionMode = await _searchExecutor
                .Execute(
                    request,
                    publish,
                    progress,
                    (exception, workItem, discoveryPath) =>
                        failures.Enqueue(new SearchRunFailure(exception, workItem, discoveryPath)),
                    cancellationToken
                )
                .ConfigureAwait(false);
            return new SearchRunOutcome(completionMode, failures.ToArray());
        }
        catch (OperationCanceledException ex)
            when (cancellationToken.IsCancellationRequested
                && (
                    ex.CancellationToken == cancellationToken || !ex.CancellationToken.CanBeCanceled
                )
            )
        {
            return new SearchRunOutcome(SearchCompletionMode.Cancelled, failures.ToArray());
        }
        catch (Exception ex)
        {
            failures.Enqueue(new SearchRunFailure(ex, null));
        }

        return new SearchRunOutcome(
            cancellationToken.IsCancellationRequested
                ? SearchCompletionMode.Cancelled
                : SearchCompletionMode.Completed,
            failures.ToArray()
        );
    }
}
