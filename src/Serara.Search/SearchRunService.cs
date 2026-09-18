using System.Collections.Concurrent;
using Serara.Core.Search;

namespace Serara.Search;

public sealed record SearchRunFailure(Exception Exception, SearchWorkItem? WorkItem);

public sealed record SearchRunOutcome(bool IsCancelled, IReadOnlyList<SearchRunFailure> Failures)
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
            return new SearchRunOutcome(true, failures.ToArray());
        }

        try
        {
            await _searchExecutor
                .Execute(
                    request,
                    publish,
                    progress,
                    (exception, workItem) =>
                        failures.Enqueue(new SearchRunFailure(exception, workItem)),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
            when (cancellationToken.IsCancellationRequested
                && (
                    ex.CancellationToken == cancellationToken || !ex.CancellationToken.CanBeCanceled
                )
            )
        {
            return new SearchRunOutcome(true, failures.ToArray());
        }
        catch (Exception ex)
        {
            failures.Enqueue(new SearchRunFailure(ex, null));
        }

        return new SearchRunOutcome(cancellationToken.IsCancellationRequested, failures.ToArray());
    }
}
