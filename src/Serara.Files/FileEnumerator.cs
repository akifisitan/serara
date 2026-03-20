using System.IO.Enumeration;

namespace Serara.Files;

public class FileSystemEnumerator
{
    private readonly EnumerationOptions _enumerationOptions = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        AttributesToSkip =
            FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
        ReturnSpecialDirectories = false,
    };

    public IEnumerable<FileSystemInfo> EnumerateFilesToFileSystemInfo<TFilter>(
        string rootDirectoryPath,
        TFilter filter,
        CancellationToken cancellationToken
    )
        where TFilter : IFileSystemEnumerationFilter
    {
        var enumeration = new FileSystemEnumerable<FileSystemInfo>(
            directory: rootDirectoryPath,
            transform: (ref FileSystemEntry entry) => entry.ToFileSystemInfo(),
            options: _enumerationOptions
        )
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return filter.IncludeEntry(ref entry);
            },
            ShouldRecursePredicate = (ref FileSystemEntry entry) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return filter.RecurseIntoEntry(ref entry);
            },
        };

        foreach (var value in enumeration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return value;
        }
    }
}
