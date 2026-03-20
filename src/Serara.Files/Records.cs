using System.IO.Compression;
using FluentValidation;

namespace Serara.Files;

public abstract record FileSearchResult
{
    public required int LineNumber { get; init; }
    public required string LineContent { get; init; }
}

public sealed record LogFileSearchResult : FileSearchResult;

public sealed record ZippedLogFileSearchResult(string RelativeFilePathInZip) : FileSearchResult;

public abstract record FileEntryWrapper
{
    public required string FilePath { get; init; }
    public required long Length { get; init; }
}

public sealed record LogFileEntryWrapper(
    DateTimeOffset EstimatedCreationTime,
    DateTimeOffset LastWriteTime
) : FileEntryWrapper
{
    public DateTime? CreationTimeUtc { get; init; }
}

public sealed record ZipFileEntryWrapper : FileEntryWrapper;

public abstract record SearchFileProgress;

public sealed record SearchFileStarted(FileEntryWrapper Entry) : SearchFileProgress;

public sealed record SearchFileFinished(
    string FilePath,
    double ElapsedMs,
    SearchFileFinishReason FinishReason
) : SearchFileProgress;

public sealed record SearchFileHeartbeat(string FilePath, double ElapsedMs) : SearchFileProgress;

public sealed record SearchZipFileEntrySkipped(ZipArchiveEntryReport Entry) : SearchFileProgress;

public sealed record SearchZipFileEntryStarted(ZipArchiveEntryReport Entry) : SearchFileProgress;

public sealed record SearchZipFileEntryHeartbeat(ZipArchiveEntryReport Entry, double ElapsedMs)
    : SearchFileProgress;

public sealed record SearchZipFileEntryFinished(
    ZipArchiveEntryReport Entry,
    double ElapsedMs,
    SearchFileFinishReason FinishReason
) : SearchFileProgress;

public enum SearchFileFinishReason
{
    EndOfFile = 0,
    EarlyExitContinueSearchingInLogFileAfterFirstResultIsFalse = 1,
    EarlyExitStopSearchingOtherFilesInZipAfterFirstResultIsTrue = 2,
}

public sealed record ZipArchiveEntryReport(
    string FilePath,
    long Length,
    DateTimeOffset EstimatedCreationTime,
    DateTimeOffset LastWriteTime
)
{
    public static implicit operator ZipArchiveEntryReport(ZipArchiveEntryWrapper entryWrapper)
    {
        return new ZipArchiveEntryReport(
            Path.Combine(entryWrapper.BaseZipFilePath, entryWrapper.ZipArchiveEntry.FullName),
            entryWrapper.ZipArchiveEntry.Length,
            entryWrapper.EstimatedCreationTime,
            entryWrapper.LastWriteTime
        );
    }
}

public sealed record ZipArchiveEntryWrapper(
    string BaseZipFilePath,
    ZipArchiveEntry ZipArchiveEntry,
    DateTimeOffset EstimatedCreationTime,
    DateTimeOffset LastWriteTime
);

public sealed record ZipFileSearchOptions
{
    public bool StopSearchingOtherFilesInZipAfterFirstResult { get; set; }
    public bool ContinueSearchingInLogFileAfterFirstResult { get; set; }
    public int StreamReaderBufferSize { get; set; } = 1024;
    public int CreationTimeBufferInSeconds { get; set; } = 5;
}

public sealed class ZipFileSearchOptionsValidator : AbstractValidator<ZipFileSearchOptions>
{
    public ZipFileSearchOptionsValidator()
    {
        RuleFor(x => x.StreamReaderBufferSize).InclusiveBetween(1024, 1024 * 10);
    }
}

public sealed record FileSearchOptions
{
    public bool ContinueSearchingInLogFileAfterFirstResult { get; set; }
    public int StreamReaderBufferSize { get; set; } = 1024;
    public int FileStreamBufferSize { get; set; } = 4096;
}

public sealed class FileSearchOptionsValidator : AbstractValidator<FileSearchOptions>
{
    public FileSearchOptionsValidator()
    {
        RuleFor(x => x.FileStreamBufferSize).InclusiveBetween(4096, 4096 * 10);
        RuleFor(x => x.StreamReaderBufferSize).InclusiveBetween(1024, 1024 * 10);
    }
}

public sealed class FileEnumeratorOptions
{
    public int CreationTimeBufferInSeconds { get; set; }
}

public interface IFileEnumerator
{
    List<FileEntryWrapper> GetFiles(
        string rootDirectoryPath,
        DateTimeOffset searchStartTime,
        DateTimeOffset searchEndTime,
        CancellationToken cancellationToken
    );
}
