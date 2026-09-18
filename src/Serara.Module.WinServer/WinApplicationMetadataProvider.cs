using Serara.Core.Metadata;

namespace Serara.Module.WinServer;

public sealed class WinApplicationMetadataProvider
{
    private const int maxAttempts = 3;

    private readonly IWinApplicationMetadataLoader _metadataLoader;
    private readonly WinApplicationMetadataMemoryCache _cache;

    internal WinApplicationMetadataProvider(
        IWinApplicationMetadataLoader metadataLoader,
        WinApplicationMetadataMemoryCache cache
    )
    {
        _metadataLoader = metadataLoader;
        _cache = cache;
    }

    public async Task<IReadOnlyList<WinApplicationMetadata>> GetMetadata(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_cache.Value is { } cachedMetadata)
        {
            return cachedMetadata;
        }

        await _cache.Lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache.Value is { } lockedCachedMetadata)
            {
                return lockedCachedMetadata;
            }

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    var metadata = await _metadataLoader
                        .Load(cancellationToken)
                        .ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    _cache.Value = metadata;
                    return metadata;
                }
                catch (MetadataLoadException ex) when (ex.IsRetryable && attempt < maxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _cache.Lock.Release();
        }
    }
}

internal sealed class WinApplicationMetadataMemoryCache
{
    public SemaphoreSlim Lock { get; } = new(1, 1);
    public IReadOnlyList<WinApplicationMetadata>? Value { get; set; }
}
