using System.Text.Json;

namespace Serara.History;

internal sealed class SearchHistoryStore : ISearchHistoryStore, IDisposable
{
    private readonly Dictionary<string, SearchHistoryEntry> _entries = [];
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _configuredFilePath;
    private string? _filePath;
    private bool _initialized;

    public SearchHistoryStore(IOptions<SeraraHistoryOptions> options)
    {
        _configuredFilePath = options.Value.FilePath;
    }

    public async Task Add(SearchHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        await EnsureInitialized(cancellationToken).ConfigureAwait(false);
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_entries.TryAdd(entry.Id, entry))
            {
                return;
            }

            try
            {
                await Store(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                _entries.Remove(entry.Id);
                throw;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public IReadOnlyList<SearchHistoryEntry> RetrieveAll() => [.. _entries.Values];

    public async Task Remove(string id, CancellationToken cancellationToken = default)
    {
        await EnsureInitialized(cancellationToken).ConfigureAwait(false);
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_entries.Remove(id, out var entry))
            {
                return;
            }

            try
            {
                await Store(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                _entries.Add(id, entry);
                throw;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task EnsureInitialized(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            await Initialize(cancellationToken).ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task Initialize(CancellationToken cancellationToken)
    {
        _filePath = Path.GetFullPath(_configuredFilePath);
        try
        {
            using var stream = File.OpenRead(_filePath);
            var entries =
                await JsonSerializer
                    .DeserializeAsync(
                        stream,
                        SearchHistoryJsonSerializerContext
                            .Default
                            .DictionaryStringSearchHistoryEntry,
                        cancellationToken
                    )
                    .ConfigureAwait(false)
                ?? throw new JsonException("Search history must contain an object.");

            foreach (var entry in entries)
            {
                _entries.TryAdd(entry.Key, entry.Value);
            }
        }
        catch (FileNotFoundException) { }
        catch (JsonException)
        {
            File.Move(_filePath, $"{_filePath}.{Guid.NewGuid():N}.corrupt");
        }
    }

    private async Task Store(CancellationToken cancellationToken)
    {
        var filePath =
            _filePath ?? throw new InvalidOperationException("History is not initialized.");
        var temporaryFilePath = Path.Combine(
            Path.GetDirectoryName(filePath)!,
            $"{Guid.NewGuid():N}.tmp"
        );
        try
        {
            using (var stream = File.Create(temporaryFilePath))
            {
                await JsonSerializer
                    .SerializeAsync(
                        stream,
                        _entries,
                        SearchHistoryJsonSerializerContext
                            .Default
                            .DictionaryStringSearchHistoryEntry,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }

            File.Move(temporaryFilePath, filePath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryFilePath);
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
