using Serara.Core.Authentication;
using Serara.Infra.Confluence;

namespace Serara.Module.WinServer;

internal sealed class WinServerMetadataLoader : IWinApplicationMetadataLoader
{
    private readonly ILogger<WinServerMetadataLoader> _logger;
    private readonly CachedMetadataLoader _cachedMetadataLoader;
    private readonly ConfluenceMetadataLoader _confluenceMetadataLoader;
    private readonly IAuthenticationHandler<BasicAuthCredential> _authenticationHandler;
    private readonly IConfluenceUserCredentialProvider _confluenceUserCredentialProvider;
    private readonly LocalDebugMetadataLoader _localDebugMetadataLoader;

    public WinServerMetadataLoader(
        ILogger<WinServerMetadataLoader> logger,
        CachedMetadataLoader cachedMetadataLoader,
        ConfluenceMetadataLoader confluenceMetadataLoader,
        IAuthenticationHandler<BasicAuthCredential> authenticationHandler,
        IConfluenceUserCredentialProvider confluenceUserCredentialProvider,
        LocalDebugMetadataLoader localDebugMetadataLoader
    )
    {
        _logger = logger;
        _cachedMetadataLoader = cachedMetadataLoader;
        _confluenceMetadataLoader = confluenceMetadataLoader;
        _authenticationHandler = authenticationHandler;
        _localDebugMetadataLoader = localDebugMetadataLoader;
        _confluenceUserCredentialProvider = confluenceUserCredentialProvider;
    }

    public async Task<IReadOnlyList<WinApplicationMetadata>> Load(
        CancellationToken cancellationToken
    )
    {
        var localDebugMetadata = await _localDebugMetadataLoader
            .Load(cancellationToken)
            .ConfigureAwait(false);

        if (localDebugMetadata.Count > 0)
        {
            return localDebugMetadata;
        }

        var localCachedMetadata = await _cachedMetadataLoader
            .Load(cancellationToken)
            .ConfigureAwait(false);

        if (localCachedMetadata.Count > 0)
        {
            return localCachedMetadata;
        }

        while (true)
        {
            try
            {
                var credentials = await _authenticationHandler
                    .RequestCredentials(cancellationToken)
                    .ConfigureAwait(false);

                _confluenceUserCredentialProvider.Set(
                    new ConfluenceUserCredential(credentials.Username, credentials.Password)
                );

                var confluenceMetadata = await _confluenceMetadataLoader
                    .Load(cancellationToken)
                    .ConfigureAwait(false);

                await CacheMetadata(confluenceMetadata, cancellationToken).ConfigureAwait(false);

                return confluenceMetadata;
            }
            catch (ConfluenceError ex)
            {
                _logger.ZLogError(ex, $"Confluence error during metadata load: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.ZLogError(
                    ex,
                    $"An unexpected error occurred while retrieving metadata from confluence"
                );
                throw;
            }
        }
    }

    private async Task CacheMetadata(
        IReadOnlyList<WinApplicationMetadata> metadata,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await _cachedMetadataLoader
                .StoreAsync(metadata.ToList(), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred while caching metadata");
        }
    }
}
