namespace Serara.Files;

public interface ILogRotation
{
    IEnumerable<(
        TEntry Entry,
        DateTimeOffset EstimatedCreationTime,
        DateTimeOffset LastWriteTime
    )> Calculate<TEntry>(
        IEnumerable<TEntry> orderedEntries,
        Func<TEntry, DateTimeOffset> getLastWriteTime,
        Func<TEntry, DateTimeOffset> getNextCreationTime
    );
}

public sealed class LogRotation : ILogRotation
{
    // Entries must be ordered oldest first. Callers retain their native timestamp
    // arithmetic when calculating the buffered start of the next interval.
    public IEnumerable<(
        TEntry Entry,
        DateTimeOffset EstimatedCreationTime,
        DateTimeOffset LastWriteTime
    )> Calculate<TEntry>(
        IEnumerable<TEntry> orderedEntries,
        Func<TEntry, DateTimeOffset> getLastWriteTime,
        Func<TEntry, DateTimeOffset> getNextCreationTime
    )
    {
        using var enumerator = orderedEntries.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            yield break;
        }

        var entry = enumerator.Current;
        var estimatedCreationTime = DateTimeOffset.MinValue;
        while (true)
        {
            yield return (entry, estimatedCreationTime, getLastWriteTime(entry));

            if (!enumerator.MoveNext())
            {
                yield break;
            }

            estimatedCreationTime = getNextCreationTime(entry);
            entry = enumerator.Current;
        }
    }
}
