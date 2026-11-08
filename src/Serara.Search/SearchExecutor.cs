using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Serara.Core.Search;

namespace Serara.Search;

public interface ISearchExecutor
{
    Task<SearchCompletionMode> Execute(
        ISearchRequest request,
        Func<ModuleSearchResult, CancellationToken, ValueTask> publish,
        Action<ModuleSearchProgress> progress,
        Action<Exception, SearchWorkItem?, string?> failure,
        CancellationToken cancellationToken
    );
}

internal sealed class SearchExecutor : ISearchExecutor
{
    private readonly ISearchBackendRegistry _registry;
    private readonly SearchExecutionOptions _options;

    public SearchExecutor(ISearchBackendRegistry registry, IOptions<SearchExecutionOptions> options)
    {
        _registry = registry;
        _options = options.Value;
    }

    public async Task<SearchCompletionMode> Execute(
        ISearchRequest request,
        Func<ModuleSearchResult, CancellationToken, ValueTask> publish,
        Action<ModuleSearchProgress> progress,
        Action<Exception, SearchWorkItem?, string?> failure,
        CancellationToken cancellationToken
    )
    {
        using var searchCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken
        );
        var backend = _registry.Get(request);
        var channel = Channel.CreateBounded<SearchWorkItem>(
            new BoundedChannelOptions(
                _options.NumConcurrentSearchOperations * _options.QueueCapacityMultiplier
            )
            {
                SingleReader = false,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait,
            }
        );

        var tasks = new Task[_options.NumConcurrentSearchOperations + 1];
        tasks[0] = Produce(
            backend,
            request,
            channel.Writer,
            progress,
            failure,
            searchCancellation.Token
        );
        for (var i = 1; i < tasks.Length; i++)
        {
            tasks[i] = Consume(
                backend,
                request,
                channel.Reader,
                publish,
                progress,
                failure,
                searchCancellation.CancelAsync,
                searchCancellation.Token
            );
        }

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
            when (searchCancellation.IsCancellationRequested
                && (
                    ex.CancellationToken == searchCancellation.Token
                    || !ex.CancellationToken.CanBeCanceled
                )
            ) { }

        cancellationToken.ThrowIfCancellationRequested();
        return searchCancellation.IsCancellationRequested
            ? SearchCompletionMode.StoppedAfterFirstMatch
            : SearchCompletionMode.Completed;
    }

    private static async Task Produce(
        ISearchBackend backend,
        ISearchRequest request,
        ChannelWriter<SearchWorkItem> writer,
        Action<ModuleSearchProgress> progress,
        Action<Exception, SearchWorkItem?, string?> failure,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await foreach (
                var item in backend
                    .Discover(
                        request,
                        progress,
                        (exception, path) => failure(exception, null, path),
                        cancellationToken
                    )
                    .ConfigureAwait(false)
            )
            {
                await writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            failure(ex, null, null);
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private async Task Consume(
        ISearchBackend backend,
        ISearchRequest request,
        ChannelReader<SearchWorkItem> reader,
        Func<ModuleSearchResult, CancellationToken, ValueTask> publish,
        Action<ModuleSearchProgress> progress,
        Action<Exception, SearchWorkItem?, string?> failure,
        Func<Task> stopSearch,
        CancellationToken cancellationToken
    )
    {
        await foreach (var item in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await foreach (
                    var result in backend
                        .Search(item, request, progress, cancellationToken)
                        .ConfigureAwait(false)
                )
                {
                    await publish(result, cancellationToken).ConfigureAwait(false);
                    if (_options.StopSearchAfterFirstMatch)
                    {
                        await stopSearch().ConfigureAwait(false);
                        break;
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failure(ex, item, null);
            }
        }
    }
}
