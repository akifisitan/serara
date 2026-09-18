using System.Diagnostics;
using System.Runtime.CompilerServices;
using Serara.Core.Search;
using Serara.Files;

namespace Serara.Module.WinServer;

public sealed class WinServerSearchBackend
    : SearchBackend<WinServerSearchRequest, WinServerSearchWorkItem, WinServerSearchSource>
{
    private const int HeartbeatIntervalMs = 15 * 1000;

    private readonly IWinApplicationPathProvider _pathProvider;
    private readonly IFileEnumerator _fileEnumerator;
    private readonly FileSearcher _fileSearcher;
    private readonly ZipFileSearcher _zipFileSearcher;
    private readonly ZipFileEntryExtractor _zipFileEntryExtractor;

    public WinServerSearchBackend(
        IWinApplicationPathProvider pathProvider,
        IFileEnumerator fileEnumerator,
        FileSearcher fileSearcher,
        ZipFileSearcher zipFileSearcher,
        ZipFileEntryExtractor zipFileEntryExtractor
    )
    {
        _pathProvider = pathProvider;
        _fileEnumerator = fileEnumerator;
        _fileSearcher = fileSearcher;
        _zipFileSearcher = zipFileSearcher;
        _zipFileEntryExtractor = zipFileEntryExtractor;
    }

    protected override async IAsyncEnumerable<WinServerSearchWorkItem> Discover(
        WinServerSearchRequest request,
        Action<ModuleSearchProgress> progress,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var metadata = request.SelectedData;
        foreach (var serverName in request.SelectedServers)
        {
            if (request.SearchStrategy != WinServerSearchStrategy.ArchiveServersOnly)
            {
                var path = _pathProvider.GetLiveServerLogPath(metadata, serverName);
                await foreach (
                    var item in DiscoverPath(path, request, progress, cancellationToken)
                        .ConfigureAwait(false)
                )
                {
                    yield return item;
                }
            }

            if (
                request.SearchStrategy != WinServerSearchStrategy.LiveServersOnly
                && _pathProvider.SupportsArchive(metadata)
            )
            {
                var path = _pathProvider.GetArchiveServerLogPath(metadata, serverName);
                await foreach (
                    var item in DiscoverPath(path, request, progress, cancellationToken)
                        .ConfigureAwait(false)
                )
                {
                    yield return item;
                }
            }
        }
    }

    protected override async IAsyncEnumerable<ModuleSearchResult<WinServerSearchSource>> Search(
        WinServerSearchWorkItem item,
        WinServerSearchRequest request,
        Action<ModuleSearchProgress> progress,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var started = Stopwatch.GetTimestamp();
        progress(new SearchWorkStarted(item.DisplayName, item.Length));

        var matcher = new StringContainsMatcher(request.SearchPattern);
        var filter = new ZipArchiveEntryFilter(request.StartTime, request.EndTime);

        switch (item.Entry)
        {
            case ZipFileEntryWrapper entry:
                await foreach (
                    var result in _zipFileSearcher
                        .SearchInZip(
                            entry,
                            matcher,
                            filter,
                            x => ReportProgress(progress, x),
                            HeartbeatIntervalMs,
                            cancellationToken
                        )
                        .ConfigureAwait(false)
                )
                {
                    var source = new WinServerZipSearchSource(entry, result, item.ApplicationName);
                    yield return new ModuleSearchResult<WinServerSearchSource>(
                        GetPath(source),
                        source
                    );
                }
                break;
            case LogFileEntryWrapper entry:
                await foreach (
                    var result in _fileSearcher
                        .SearchInFile(
                            entry,
                            matcher,
                            x => ReportProgress(progress, x),
                            HeartbeatIntervalMs,
                            cancellationToken
                        )
                        .ConfigureAwait(false)
                )
                {
                    var source = new WinServerLogSearchSource(entry, result, item.ApplicationName);
                    yield return new ModuleSearchResult<WinServerSearchSource>(
                        GetPath(source),
                        source
                    );
                }
                break;
            default:
                throw new NotSupportedException(
                    $"Unsupported WinServer entry '{item.Entry.GetType()}'."
                );
        }

        progress(new SearchWorkFinished(item.DisplayName, Stopwatch.GetElapsedTime(started)));
    }

    protected override async Task<string> Materialize(
        WinServerSearchSource source,
        CancellationToken cancellationToken
    )
    {
        return source switch
        {
            WinServerZipSearchSource zipped => await ExtractZipEntry(zipped, cancellationToken)
                .ConfigureAwait(false),
            WinServerLogSearchSource log => MaterializeLog(log, cancellationToken),
            _ => throw new NotSupportedException($"Unsupported source '{source.GetType()}'."),
        };
    }

    private async IAsyncEnumerable<WinServerSearchWorkItem> DiscoverPath(
        string path,
        WinServerSearchRequest request,
        Action<ModuleSearchProgress> progress,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var started = Stopwatch.GetTimestamp();
        progress(new SearchDiscoveryStarted(path));
        var entries = await Task.Run(
                () =>
                    _fileEnumerator.GetFiles(
                        path,
                        request.StartTime,
                        request.EndTime,
                        cancellationToken
                    ),
                cancellationToken
            )
            .ConfigureAwait(false);
        progress(
            new SearchDiscoveryFinished(path, entries.Count, Stopwatch.GetElapsedTime(started))
        );

        foreach (var entry in entries)
        {
            yield return new WinServerSearchWorkItem(
                entry,
                Path.GetRelativePath(path, entry.FilePath),
                request.SelectedData.Name
            )
            {
                DisplayName = entry.FilePath,
                Length = entry.Length,
            };
        }
    }

    private static string MaterializeLog(
        WinServerLogSearchSource source,
        CancellationToken cancellationToken
    )
    {
        var entry = source.Entry;
        var path =
            FindCurrentLogPath(entry, cancellationToken)
            ?? throw new FileNotFoundException(
                "Could not locate the log after rotation.",
                entry.FilePath
            );

        return path;
    }

    private async Task<string> ExtractZipEntry(
        WinServerZipSearchSource source,
        CancellationToken cancellationToken
    )
    {
        var archiveName = source
            .Entry.FilePath.TrimStart('\\')
            .Replace(Path.DirectorySeparatorChar.ToString(), "__")
            .Replace(":", string.Empty);
        var outputDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "SearchResults",
            source.ApplicationName,
            Path.GetFileNameWithoutExtension(archiveName)
        );
        Directory.CreateDirectory(outputDirectory);
        var outputPath = ZipFileEntryExtractor.GetOutputFilePath(
            outputDirectory,
            source.Result.RelativeFilePathInZip
        );
        return File.Exists(outputPath)
            ? outputPath
            : await _zipFileEntryExtractor
                .ExtractEntry(
                    source.Entry.FilePath,
                    source.Result.RelativeFilePathInZip,
                    outputDirectory,
                    cancellationToken
                )
                .ConfigureAwait(false);
    }

    private static string? FindCurrentLogPath(
        LogFileEntryWrapper entry,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current = new FileInfo(entry.FilePath);
        if (
            current.Exists
            && entry.Length == current.Length
            && entry.LastWriteTime == current.LastWriteTime
        )
        {
            return entry.FilePath;
        }

        return
            current.Exists
            && entry.CreationTimeUtc is { } created
            && created == current.CreationTimeUtc
            && current.Length >= entry.Length
            && current.LastWriteTime >= entry.LastWriteTime
            ? entry.FilePath
            : null;
    }

    private static string GetPath(WinServerSearchSource source) =>
        source switch
        {
            WinServerZipSearchSource zipped => Path.Combine(
                zipped.Entry.FilePath,
                zipped.Result.RelativeFilePathInZip
            ),
            WinServerLogSearchSource log => log.Entry.FilePath,
            _ => throw new NotSupportedException(),
        };

    private static void ReportProgress(
        Action<ModuleSearchProgress> progress,
        SearchFileProgress value
    )
    {
        if (value is SearchFileHeartbeat heartbeat)
        {
            progress(
                new SearchWorkHeartbeat(
                    heartbeat.FilePath,
                    TimeSpan.FromMilliseconds(heartbeat.ElapsedMs)
                )
            );
        }
    }

    private sealed class StringContainsMatcher(string pattern) : ILineMatcher
    {
        public bool IsMatch(ReadOnlySpan<char> line) =>
            line.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ZipArchiveEntryFilter(DateTimeOffset start, DateTimeOffset end)
        : IZipArchiveEntryFilter
    {
        public bool Include(ZipArchiveEntryWrapper entry) =>
            Path.GetExtension(entry.ZipArchiveEntry.FullName)
                .Equals(".log", StringComparison.OrdinalIgnoreCase)
            && start <= entry.LastWriteTime
            && entry.EstimatedCreationTime <= end;
    }
}
