using System.Globalization;
using System.IO.Enumeration;
using Serara.Files;

namespace Serara.Module.WinServer;

public sealed class WinFileEnumerator : IFileEnumerator
{
    private readonly FileEnumeratorOptions _options;
    private readonly ILogRotation _logRotation;
    private readonly EnumerationOptions _enumerationOptions = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        AttributesToSkip =
            FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
        ReturnSpecialDirectories = false,
    };

    public WinFileEnumerator(IOptions<FileEnumeratorOptions> options, ILogRotation logRotation)
    {
        _options = options.Value;
        _logRotation = logRotation;
    }

    public List<FileEntryWrapper> GetFiles(
        string rootDirectoryPath,
        DateTimeOffset searchStartTime,
        DateTimeOffset searchEndTime,
        CancellationToken cancellationToken
    )
    {
        var result = new List<FileEntryWrapper>();

        // We need creationTime
        // We assume file traversal is much less expensive than searching inside the file itself
        // Given a rootDirectoryPath in a server
        // Search all directories for log
        // Need to find a way to sort these entries and set their creationTime properly
        // {baseUrl}\HH\dd_MM_yyyy.log
        // {baseUrl}\HH\dd_MM_yyyy.1.log
        // {baseUrl}\HH\dd_MM_yyyy.2.log
        // {baseUrl}\trace.*.1.log
        // {baseUrl}\error.*.1.log
        var enumeration = new FileSystemEnumerable<FileInfo>(
            directory: rootDirectoryPath,
            transform: (ref FileSystemEntry entry) => (FileInfo)entry.ToFileSystemInfo(),
            options: _enumerationOptions
        )
        {
            ShouldRecursePredicate = (ref FileSystemEntry entry) => true,
            ShouldIncludePredicate = (ref FileSystemEntry entry) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry.IsDirectory)
                {
                    return false;
                }

                var fileExtension = Path.GetExtension(entry.FileName);

                if (fileExtension.Equals(".log", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!fileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (
                    !DateTimeOffset.TryParseExact(
                        Path.GetFileNameWithoutExtension(entry.FileName),
                        "dd.MM.yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var zipFileTime
                    )
                )
                {
                    return false;
                }

                return searchStartTime.Date <= zipFileTime.Date
                    && zipFileTime.Date <= searchEndTime.Date;
            },
        };

        var directoryPathMap = new Dictionary<string, List<FileInfo>>();

        foreach (var fileInfo in enumeration)
        {
            if (
                Path.GetExtension(fileInfo.FullName)
                    .Equals(".zip", StringComparison.OrdinalIgnoreCase)
            )
            {
                result.Add(
                    new ZipFileEntryWrapper
                    {
                        FilePath = fileInfo.FullName,
                        Length = fileInfo.Length,
                    }
                );
                continue;
            }

            var directoryPath = Path.GetDirectoryName(fileInfo.FullName)!;

            if (directoryPathMap.TryGetValue(directoryPath, out var filePathList))
            {
                filePathList.Add(fileInfo);
            }
            else
            {
                directoryPathMap.Add(directoryPath, [fileInfo]);
            }
        }

        foreach (var filePathList in directoryPathMap.Values)
        {
            foreach (var entry in GetSortedEntries(filePathList))
            {
                if (
                    searchStartTime <= entry.LastWriteTime
                    && entry.EstimatedCreationTime <= searchEndTime
                )
                {
                    result.Add(entry);
                }
            }
        }

        return result;
    }

    // Entries in a single directory, should not be used with a FileSystemEnumerable that iterates over multiple directories
    private List<LogFileEntryWrapper> GetSortedEntries(List<FileInfo> entries)
    {
        var logGroups = new Dictionary<string, List<FileInfo>>();

        // Create groups
        foreach (var entry in entries)
        {
            var entryFileName = Path.GetFileName(entry.FullName);
            var dotIdx = entryFileName.IndexOf('.');
            var logGroupName = entryFileName[..dotIdx];

            if (logGroups.TryGetValue(logGroupName, out var group))
            {
                group.Add(entry);
            }
            else
            {
                logGroups.Add(logGroupName, [entry]);
            }
        }

        var sortedEntries = new List<LogFileEntryWrapper>();

        foreach (var key in logGroups.Keys)
        {
            foreach (
                var (entry, estimatedCreationTime, lastWriteTime) in _logRotation.Calculate(
                    logGroups[key].OrderBy(entry => entry.LastWriteTime),
                    entry => entry.LastWriteTime,
                    entry =>
                        entry.LastWriteTime.Subtract(
                            TimeSpan.FromSeconds(_options.CreationTimeBufferInSeconds)
                        )
                )
            )
            {
                sortedEntries.Add(
                    new LogFileEntryWrapper(estimatedCreationTime, lastWriteTime)
                    {
                        FilePath = entry.FullName,
                        Length = entry.Length,
                        CreationTimeUtc = entry.CreationTimeUtc,
                    }
                );
            }
        }

        return sortedEntries;
    }
}
