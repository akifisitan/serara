using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Serara.Core.Search;

namespace Serara.Search;

public interface ISearchExecutor
{
    Task Execute(
        ISearchRequest request,
        Func<ModuleSearchResult, CancellationToken, ValueTask> publish,
        Action<ModuleSearchProgress> progress,
        Action<Exception, SearchWorkItem?> failure,
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

    public async Task Execute(
        ISearchRequest request,
        Func<ModuleSearchResult, CancellationToken, ValueTask> publish,
        Action<ModuleSearchProgress> progress,
        Action<Exception, SearchWorkItem?> failure,
        CancellationToken cancellationToken
    )
    {
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

        var producer = Produce(
            backend,
            request,
            channel.Writer,
            progress,
            failure,
            cancellationToken
        );
        var consumers = Enumerable
            .Range(0, _options.NumConcurrentSearchOperations)
            .Select(_ =>
                Consume(
                    backend,
                    request,
                    channel.Reader,
                    publish,
                    progress,
                    failure,
                    cancellationToken
                )
            );

        await Task.WhenAll(consumers.Prepend(producer)).ConfigureAwait(false);
    }

    private static async Task Produce(
        ISearchBackend backend,
        ISearchRequest request,
        ChannelWriter<SearchWorkItem> writer,
        Action<ModuleSearchProgress> progress,
        Action<Exception, SearchWorkItem?> failure,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await foreach (
                var item in backend
                    .Discover(request, progress, cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                await writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            failure(ex, null);
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private static async Task Consume(
        ISearchBackend backend,
        ISearchRequest request,
        ChannelReader<SearchWorkItem> reader,
        Func<ModuleSearchResult, CancellationToken, ValueTask> publish,
        Action<ModuleSearchProgress> progress,
        Action<Exception, SearchWorkItem?> failure,
        CancellationToken cancellationToken
    )
    {
        await foreach (var item in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                await foreach (
                    var result in backend
                        .Search(item, request, progress, cancellationToken)
                        .ConfigureAwait(false)
                )
                {
                    await publish(result, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failure(ex, item);
            }
        }
    }
}
