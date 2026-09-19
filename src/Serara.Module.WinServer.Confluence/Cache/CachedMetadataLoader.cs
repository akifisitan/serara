namespace Serara.Module.WinServer;

internal sealed class CachedMetadataLoader : IWinApplicationMetadataLoader
{
    private readonly ILogger<CachedMetadataLoader> _logger;
    private readonly JsonFileStore _store;

    public const string CacheFileName = "cached-metadata.json";

    public CachedMetadataLoader(ILogger<CachedMetadataLoader> logger, JsonFileStore store)
    {
        _logger = logger;
        _store = store;
    }

    public async Task<IReadOnlyList<WinApplicationMetadata>> Load(
        CancellationToken cancellationToken
    )
    {
        try
        {
            var jsonContent = await _store
                .ReadAsync(
                    CacheFileName,
                    WinServerJsonSerializerContext.Default.ListWinApplicationMetadata,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (jsonContent is null)
            {
                _logger.ZLogWarning($"Cached metadata file is not in an expected format.");
                return [];
            }

            return jsonContent;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (FileNotFoundException)
        {
            _logger.ZLogInformation($"Cached metadata file {CacheFileName} not found.");
            return [];
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred while loading cached metadata.");
            return [];
        }
    }

    public Task StoreAsync(
        List<WinApplicationMetadata> metadata,
        CancellationToken cancellationToken
    ) =>
        _store.WriteAsync(
            CacheFileName,
            metadata,
            WinServerJsonSerializerContext.Default.ListWinApplicationMetadata,
            cancellationToken
        );
}
