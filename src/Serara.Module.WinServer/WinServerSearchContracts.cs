using Serara.Core.Search;
using Serara.Files;

namespace Serara.Module.WinServer;

public sealed record WinServerSearchRequest(
    WinApplicationMetadata SelectedData,
    IReadOnlyList<string> SelectedServers,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string SearchPattern,
    WinServerSearchStrategy SearchStrategy
) : ISearchRequest;

public enum WinServerSearchStrategy
{
    Default = 0,
    LiveServersOnly = 1,
    ArchiveServersOnly = 2,
    LiveAndArchiveServers = 3,
}

public sealed record WinServerSearchWorkItem(
    FileEntryWrapper Entry,
    string RelativePath,
    string ApplicationName
) : SearchWorkItem;

public abstract record WinServerSearchSource(string ApplicationName) : SearchSourceReference;

public sealed record WinServerLogSearchSource(
    LogFileEntryWrapper Entry,
    LogFileSearchResult Result,
    string ApplicationName
) : WinServerSearchSource(ApplicationName);

public sealed record WinServerZipSearchSource(
    ZipFileEntryWrapper Entry,
    ZippedLogFileSearchResult Result,
    string ApplicationName
) : WinServerSearchSource(ApplicationName);
