using System.IO.Enumeration;

namespace Serara.Files;

public interface IFileSystemEnumerationFilter
{
    bool IncludeEntry(ref FileSystemEntry entry);
    bool RecurseIntoEntry(ref FileSystemEntry entry);
}

public interface ILineMatcher
{
    bool IsMatch(ReadOnlySpan<char> line);
}

public interface IZipArchiveEntryFilter
{
    bool Include(ZipArchiveEntryWrapper entry);
}
