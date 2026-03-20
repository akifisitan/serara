using System.Runtime.CompilerServices;

namespace Serara.Core.Search;

public interface ISearchRequest;

public abstract record SearchWorkItem
{
    public required string DisplayName { get; init; }
    public long? Length { get; init; }
}

public abstract record SearchSourceReference;

public sealed record ModuleSearchResult(string Summary, SearchSourceReference Source);

public sealed record ModuleSearchResult<TSource>(string Summary, TSource Source)
    where TSource : SearchSourceReference;

public abstract record ModuleSearchProgress(string DisplayName);

public sealed record SearchDiscoveryStarted(string Name) : ModuleSearchProgress(Name);

public sealed record SearchDiscoveryFinished(string Name, int ItemCount, TimeSpan Elapsed)
    : ModuleSearchProgress(Name);

public sealed record SearchWorkStarted(string Name, long? Length) : ModuleSearchProgress(Name);

public sealed record SearchWorkHeartbeat(string Name, TimeSpan Elapsed)
    : ModuleSearchProgress(Name);

public sealed record SearchWorkFinished(string Name, TimeSpan Elapsed) : ModuleSearchProgress(Name);

public interface ISearchBackend
{
    Type RequestType { get; }
    Type SourceType { get; }

    IAsyncEnumerable<SearchWorkItem> Discover(
        ISearchRequest request,
        Action<ModuleSearchProgress> progress,
        CancellationToken cancellationToken
    );

    IAsyncEnumerable<ModuleSearchResult> Search(
        SearchWorkItem item,
        ISearchRequest request,
        Action<ModuleSearchProgress> progress,
        CancellationToken cancellationToken
    );

    Task<string> Materialize(SearchSourceReference source, CancellationToken cancellationToken);
}

public abstract class SearchBackend<TRequest, TWorkItem, TSource> : ISearchBackend
    where TRequest : ISearchRequest
    where TWorkItem : SearchWorkItem
    where TSource : SearchSourceReference
{
    public Type RequestType => typeof(TRequest);
    public Type SourceType => typeof(TSource);

    public async IAsyncEnumerable<SearchWorkItem> Discover(
        ISearchRequest request,
        Action<ModuleSearchProgress> progress,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await foreach (
            var item in Discover(GetRequest(request), progress, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<ModuleSearchResult> Search(
        SearchWorkItem item,
        ISearchRequest request,
        Action<ModuleSearchProgress> progress,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await foreach (
            var result in Search(
                    GetWorkItem(item),
                    GetRequest(request),
                    progress,
                    cancellationToken
                )
                .ConfigureAwait(false)
        )
        {
            yield return new ModuleSearchResult(result.Summary, result.Source);
        }
    }

    public Task<string> Materialize(
        SearchSourceReference source,
        CancellationToken cancellationToken
    ) => Materialize(GetSource(source), cancellationToken);

    protected abstract IAsyncEnumerable<TWorkItem> Discover(
        TRequest request,
        Action<ModuleSearchProgress> progress,
        CancellationToken cancellationToken
    );

    protected abstract IAsyncEnumerable<ModuleSearchResult<TSource>> Search(
        TWorkItem item,
        TRequest request,
        Action<ModuleSearchProgress> progress,
        CancellationToken cancellationToken
    );

    protected abstract Task<string> Materialize(
        TSource source,
        CancellationToken cancellationToken
    );

    private TRequest GetRequest(ISearchRequest request) =>
        request is TRequest typed
            ? typed
            : throw new ArgumentException(
                $"Backend '{GetType()}' cannot handle request type '{request.GetType()}'.",
                nameof(request)
            );

    private TWorkItem GetWorkItem(SearchWorkItem item) =>
        item is TWorkItem typed
            ? typed
            : throw new ArgumentException(
                $"Backend '{GetType()}' cannot handle work item type '{item.GetType()}'.",
                nameof(item)
            );

    private TSource GetSource(SearchSourceReference source) =>
        source is TSource typed
            ? typed
            : throw new ArgumentException(
                $"Backend '{GetType()}' cannot handle source type '{source.GetType()}'.",
                nameof(source)
            );
}
