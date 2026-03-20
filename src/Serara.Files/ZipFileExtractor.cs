using System.IO.Compression;

namespace Serara.Files;

public sealed class ZipFileEntryExtractor
{
    public async Task<string> ExtractEntry(
        string zipFilePath,
        string entryName,
        string outputDirectoryPath,
        CancellationToken cancellationToken
    )
    {
        var zipArchive = await ZipFile
            .OpenReadAsync(zipFilePath, cancellationToken)
            .ConfigureAwait(false);

        await using var zipArchiveScope = zipArchive.ConfigureAwait(false);

        var entry =
            zipArchive.GetEntry(entryName)
            ?? throw new InvalidOperationException(
                $"{nameof(entryName)}: {entryName} does not exist in {zipFilePath} archive"
            );

        var outputFilePath = GetOutputFilePath(outputDirectoryPath, entryName);
        var parentDirectory = Path.GetDirectoryName(outputFilePath)!;
        Directory.CreateDirectory(parentDirectory);
        var temporaryFilePath = Path.Combine(parentDirectory, $"{Guid.NewGuid():N}.tmp");

        try
        {
            await entry
                .ExtractToFileAsync(temporaryFilePath, overwrite: false, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryFilePath, outputFilePath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryFilePath);
        }

        return outputFilePath;
    }

    public static string GetOutputFilePath(string outputDirectoryPath, string entryName)
    {
        var outputDirectory = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(outputDirectoryPath)
        );
        var relativeEntryPath = entryName
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        if (
            string.IsNullOrWhiteSpace(relativeEntryPath)
            || Path.IsPathRooted(relativeEntryPath)
            || relativeEntryPath.Contains(':')
            || Path.EndsInDirectorySeparator(relativeEntryPath)
        )
        {
            throw new InvalidDataException($"Invalid archive entry path: {entryName}");
        }

        var outputFilePath = Path.GetFullPath(Path.Combine(outputDirectory, relativeEntryPath));
        var directoryPrefix = Path.EndsInDirectorySeparator(outputDirectory)
            ? outputDirectory
            : outputDirectory + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!outputFilePath.StartsWith(directoryPrefix, comparison))
        {
            throw new InvalidDataException(
                $"Archive entry is outside the output directory: {entryName}"
            );
        }

        return outputFilePath;
    }
}
