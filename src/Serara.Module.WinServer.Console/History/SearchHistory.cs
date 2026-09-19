using System.Text.Json;
using System.Text.Json.Serialization;
using Serara.Files;
using Serara.History;

namespace Serara.Module.WinServer.Console;

internal sealed record WinServerSearchHistoryPayload(
    string ApplicationName,
    IReadOnlyList<string> SelectedServers,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string SearchPattern,
    WinServerSearchStrategy ServerSearchStrategy
);

internal sealed record WinServerSearchHistoryEntry(
    string Id,
    DateTimeOffset CreatedAt,
    WinServerSearchHistoryPayload Payload
);

[JsonSourceGenerationOptions(
    RespectNullableAnnotations = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    UseStringEnumConverter = true
)]
[JsonSerializable(typeof(WinServerSearchHistoryPayload))]
internal sealed partial class WinServerSearchHistoryJsonSerializerContext : JsonSerializerContext;

internal sealed class SearchHistory
{
    private const string capabilityId = "win-server-search";

    private readonly ISearchHistoryStore _store;
    private readonly ITimeProvider _timeProvider;

    public SearchHistory(ISearchHistoryStore store, ITimeProvider timeProvider)
    {
        _store = store;
        _timeProvider = timeProvider;
    }

    public async Task Add(
        WinServerSearchRequest value,
        CancellationToken cancellationToken = default
    )
    {
        var createdAt = _timeProvider.LocalNow();
        var historyPayload = new WinServerSearchHistoryPayload(
            value.SelectedData.Name,
            [.. value.SelectedServers],
            value.StartTime,
            value.EndTime,
            value.SearchPattern,
            value.SearchStrategy
        );
        var payload = JsonSerializer.SerializeToElement(
            historyPayload,
            WinServerSearchHistoryJsonSerializerContext.Default.WinServerSearchHistoryPayload
        );
        await _store
            .Add(
                new SearchHistoryEntry(
                    Guid.NewGuid().ToString("N"),
                    $"{value.SelectedData.Name} - {value.SearchPattern}",
                    createdAt,
                    capabilityId,
                    $"{value.SelectedData.Name} - {value.SearchPattern}",
                    payload
                ),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public IReadOnlyList<WinServerSearchHistoryEntry> RetrieveAll()
    {
        var records = new List<WinServerSearchHistoryEntry>();
        foreach (
            var entry in _store.RetrieveAll().Where(entry => entry.CapabilityId == capabilityId)
        )
        {
            try
            {
                var payload = JsonSerializer.Deserialize(
                    entry.Payload,
                    WinServerSearchHistoryJsonSerializerContext
                        .Default
                        .WinServerSearchHistoryPayload
                );
                if (payload is not null)
                {
                    records.Add(
                        new WinServerSearchHistoryEntry(entry.Id, entry.CreatedAt, payload)
                    );
                }
            }
            catch (JsonException)
            {
                // Ignore records written by incompatible module versions.
            }
        }

        return records;
    }

    public Task EnsureInitialized(CancellationToken cancellationToken = default) =>
        _store.EnsureInitialized(cancellationToken);
}
