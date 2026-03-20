using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace Serara.Files;

public sealed class ZipFileSearcher
{
    private readonly ZipFileSearchOptions _options;

    public ZipFileSearcher(IOptions<ZipFileSearchOptions> options)
    {
        _options = options.Value;
    }

    public async IAsyncEnumerable<ZippedLogFileSearchResult> SearchInZip<
        TLineMatcher,
        TZipArchiveEntryFilter
    >(
        FileEntryWrapper fileEntry,
        TLineMatcher lineMatcher,
        TZipArchiveEntryFilter zipArchiveEntryFilter,
        Action<SearchFileProgress>? progress = null,
        int heartbeatIntervalMs = 15 * 1000,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
        where TLineMatcher : ILineMatcher
        where TZipArchiveEntryFilter : IZipArchiveEntryFilter
    {
        var zipArchive = await ZipFile
            .OpenReadAsync(fileEntry.FilePath, cancellationToken)
            .ConfigureAwait(false);

        await using var zipArchiveScope = zipArchive.ConfigureAwait(false);

        var entries = GetSortedEntries(zipArchive, fileEntry.FilePath);

        var tsStart = Stopwatch.GetTimestamp();

        progress?.Invoke(new SearchFileStarted(fileEntry));

        foreach (var entryWrapper in entries)
        {
            if (!zipArchiveEntryFilter.Include(entryWrapper))
            {
                progress?.Invoke(new SearchZipFileEntrySkipped(entryWrapper));
                continue;
            }

            var entry = entryWrapper.ZipArchiveEntry;

            using var stream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var reader = new StreamReader(
                stream,
                bufferSize: _options.StreamReaderBufferSize
            );

            progress?.Invoke(new SearchZipFileEntryStarted(entryWrapper));

            string? line = null;
            var lineNumber = 0;
            var numHeartbeats = 0;
            var foundMatch = false;
            var finishReason = SearchFileFinishReason.EndOfFile;

            var tsReadStart = Stopwatch.GetTimestamp();

            while (
                (line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null
            )
            {
                var elapsedMs = tsReadStart.GetElapsedTotalMs();

                if (elapsedMs >= heartbeatIntervalMs * (numHeartbeats + 1))
                {
                    numHeartbeats++;
                    progress?.Invoke(new SearchZipFileEntryHeartbeat(entryWrapper, elapsedMs));
                }

                lineNumber++;

                if (lineMatcher.IsMatch(line))
                {
                    foundMatch = true;
                    yield return new ZippedLogFileSearchResult(entry.FullName)
                    {
                        LineNumber = lineNumber,
                        LineContent = line,
                    };

                    if (!_options.ContinueSearchingInLogFileAfterFirstResult)
                    {
                        finishReason =
                            SearchFileFinishReason.EarlyExitContinueSearchingInLogFileAfterFirstResultIsFalse;
                        break;
                    }
                }
            }

            progress?.Invoke(
                new SearchZipFileEntryFinished(
                    entryWrapper,
                    tsReadStart.GetElapsedTotalMs(),
                    finishReason
                )
            );

            if (foundMatch && _options.StopSearchingOtherFilesInZipAfterFirstResult)
            {
                progress?.Invoke(
                    new SearchFileFinished(
                        fileEntry.FilePath,
                        tsStart.GetElapsedTotalMs(),
                        SearchFileFinishReason.EarlyExitStopSearchingOtherFilesInZipAfterFirstResultIsTrue
                    )
                );
                yield break;
            }
        }

        progress?.Invoke(
            new SearchFileFinished(
                fileEntry.FilePath,
                tsStart.GetElapsedTotalMs(),
                SearchFileFinishReason.EndOfFile
            )
        );
    }

    private List<ZipArchiveEntryWrapper> GetSortedEntries(ZipArchive zipArchive, string zipFilePath)
    {
        var logGroups = new Dictionary<string, List<ZipArchiveEntry>>();

        // Create groups
        foreach (var entry in zipArchive.Entries)
        {
            var entryPath = entry.FullName.Replace('\\', '/');
            if (entryPath.EndsWith('/'))
            {
                continue;
            }

            var entryFileName = Path.GetFileName(entryPath);
            var dotIdx = entryFileName.IndexOf('.');
            var logGroupName = Path.Combine(
                Path.GetDirectoryName(entryPath) ?? string.Empty,
                dotIdx >= 0 ? entryFileName[..dotIdx] : entryFileName
            );

            if (logGroups.TryGetValue(logGroupName, out var group))
            {
                group.Add(entry);
            }
            else
            {
                logGroups.Add(logGroupName, [entry]);
            }
        }

        var sortedEntries = new List<ZipArchiveEntryWrapper>();

        foreach (var key in logGroups.Keys)
        {
            // Sort each group by last write time ascending
            // use n th entry's lastWriteTime as n + 1 th entry's creationTime
            var sortedGroup = logGroups[key].OrderBy(x => x.LastWriteTime).ToList();

            var firstEntry = sortedGroup[0];
            sortedEntries.Add(
                new ZipArchiveEntryWrapper(
                    zipFilePath,
                    firstEntry,
                    DateTimeOffset.MinValue,
                    firstEntry.LastWriteTime
                )
            );

            for (var i = 0; i < sortedGroup.Count - 1; i++)
            {
                var currentEntry = sortedGroup[i];
                var nextEntry = sortedGroup[i + 1];

                // Handle next entry
                sortedEntries.Add(
                    new ZipArchiveEntryWrapper(
                        zipFilePath,
                        nextEntry,
                        currentEntry.LastWriteTime.Subtract(
                            TimeSpan.FromSeconds(_options.CreationTimeBufferInSeconds)
                        ),
                        nextEntry.LastWriteTime
                    )
                );
            }
        }

        return sortedEntries;
    }
}
