using Microsoft.Extensions.Configuration;

namespace Serara.Module.WinServer;

internal sealed class LocalDebugMetadataLoader : IWinApplicationMetadataLoader
{
    private const string FileName = "local-metadata.json";

    private readonly JsonFileStore _store;
    private readonly ILogger<LocalDebugMetadataLoader> _logger;
    private readonly IConfiguration _configuration;

    public LocalDebugMetadataLoader(
        JsonFileStore store,
        ILogger<LocalDebugMetadataLoader> logger,
        IConfiguration configuration
    )
    {
        _store = store;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<WinApplicationMetadata>> Load(
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (!_configuration.GetSection("Debug").GetValue<bool>("IsEnabled"))
            {
                return [];
            }

            var data = await _store
                .ReadAsync(
                    FileName,
                    WinServerJsonSerializerContext.Default.ListWinApplicationMetadata,
                    cancellationToken
                )
                .ConfigureAwait(false);

            return data ?? [];
        }
        catch (FileNotFoundException)
        {
            return [];
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred while loading local debug data");
            return [];
        }
    }
}
