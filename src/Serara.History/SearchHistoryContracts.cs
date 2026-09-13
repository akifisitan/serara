using System.Text.Json;

namespace Serara.History;

public sealed record SearchHistoryEntry(
    string Id,
    string Name,
    DateTimeOffset CreatedAt,
    string CapabilityId,
    string Display,
    JsonElement Payload
);

public interface ISearchHistoryStore
{
    Task Add(SearchHistoryEntry entry, CancellationToken cancellationToken);
    Task Remove(string id, CancellationToken cancellationToken);
    Task EnsureInitialized(CancellationToken cancellationToken);
    IReadOnlyList<SearchHistoryEntry> RetrieveAll();
}
