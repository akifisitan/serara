namespace Serara.Module.WinServer;

public interface IWinApplicationPathProvider
{
    string GetLiveServerLogPath(WinApplicationMetadata metadata, string serverName);
    string GetArchiveServerLogPath(WinApplicationMetadata metadata, string serverName);
    bool SupportsArchive(WinApplicationMetadata metadata);
    bool SupportsArchiveSearch { get; }
}
