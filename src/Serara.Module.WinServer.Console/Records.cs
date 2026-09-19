using System.IO.Enumeration;
using Serara.Files;
using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed record OpenInExplorerEvent(string Path) : ICustomPromptEvent;

internal enum SearchOptionChoice
{
    Default = 0,
    FromHistory = 1,
    NewRun = 2,
}

internal enum SearchHistoryResolution
{
    Complete,
    MissingApplication,
    MissingServers,
}

internal enum SearchRequestResolution
{
    Complete,
    InvalidApplication,
    InvalidServers,
    InvalidSearchStrategy,
    InvalidTimeRange,
    InvalidSearchPattern,
    RejectedHistory,
    RejectedNewRun,
}

internal sealed record FileTraversal(bool MoveUp, FileSystemInfo FileSystemInfo);

internal sealed record LogFileExploreFilter : IFileSystemEnumerationFilter
{
    public bool RecurseIntoEntry(ref FileSystemEntry entry) => false;

    public bool IncludeEntry(ref FileSystemEntry entry) =>
        entry.IsDirectory
        || entry.FileName.EndsWith(".log", StringComparison.OrdinalIgnoreCase)
        || entry.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
}
