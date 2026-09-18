namespace Serara.Module.WinServer;

public sealed class WinApplicationPathProvider : IWinApplicationPathProvider
{
    private readonly WinServerModuleOptions _options;

    public WinApplicationPathProvider(IOptions<WinServerModuleOptions> options)
    {
        _options = options.Value;
    }

    public bool SupportsArchiveSearch => !string.IsNullOrWhiteSpace(_options.ArchiveServerBasePath);

    public string GetLiveServerLogPath(WinApplicationMetadata metadata, string serverName)
    {
        return Path.Combine(
            _options.LiveServerBasePath,
            serverName,
            _options.LogPathPrefix,
            metadata.LogPath
        );
    }

    public string GetArchiveServerLogPath(WinApplicationMetadata metadata, string serverName)
    {
        return Path.Combine(
            _options.ArchiveServerBasePath,
            metadata.ArchiveDirectoryName,
            serverName.ToUpperInvariant(),
            _options.LogPathPrefix,
            metadata.LogPath
        );
    }

    public bool SupportsArchive(WinApplicationMetadata metadata) =>
        !string.IsNullOrWhiteSpace(metadata.ArchiveDirectoryName);
}
