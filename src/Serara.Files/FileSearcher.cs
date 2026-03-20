using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Serara.Files;

public sealed class FileSearcher
{
    private readonly FileSearchOptions _options;

    public FileSearcher(IOptions<FileSearchOptions> options)
    {
        _options = options.Value;
    }

    public async IAsyncEnumerable<LogFileSearchResult> SearchInFile<TMatcher>(
        FileEntryWrapper fileEntry,
        TMatcher matcher,
        Action<SearchFileProgress>? progress = null,
        int heartbeatIntervalMs = 15 * 1000,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
        where TMatcher : ILineMatcher
    {
        using var stream = new FileStream(
            fileEntry.FilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            _options.FileStreamBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan
        );

        using var reader = new StreamReader(stream, bufferSize: _options.StreamReaderBufferSize);

        string? line = null;
        var lineNumber = 0;
        var numHeartbeats = 0;

        var tsReadStart = Stopwatch.GetTimestamp();

        progress?.Invoke(new SearchFileStarted(fileEntry));

        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            var elapsedMs = tsReadStart.GetElapsedTotalMs();

            if (elapsedMs >= heartbeatIntervalMs * (numHeartbeats + 1))
            {
                numHeartbeats++;
                progress?.Invoke(new SearchFileHeartbeat(fileEntry.FilePath, elapsedMs));
            }

            lineNumber++;

            if (matcher.IsMatch(line))
            {
                yield return new LogFileSearchResult
                {
                    LineNumber = lineNumber,
                    LineContent = line,
                };

                if (!_options.ContinueSearchingInLogFileAfterFirstResult)
                {
                    progress?.Invoke(
                        new SearchFileFinished(
                            fileEntry.FilePath,
                            tsReadStart.GetElapsedTotalMs(),
                            SearchFileFinishReason.EarlyExitContinueSearchingInLogFileAfterFirstResultIsFalse
                        )
                    );

                    yield break;
                }
            }
        }

        progress?.Invoke(
            new SearchFileFinished(
                fileEntry.FilePath,
                tsReadStart.GetElapsedTotalMs(),
                SearchFileFinishReason.EndOfFile
            )
        );
    }
}
