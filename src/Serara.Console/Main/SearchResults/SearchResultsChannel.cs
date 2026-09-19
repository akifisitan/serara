using Serara.Core.Search;

namespace Serara.Console;

internal sealed class SearchResultsChannel
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, ModuleSearchResult> _results = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal
    );
    private readonly Channel<bool> _available = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest }
    );
    private readonly int _maxResults;
    private IReadOnlyList<ModuleSearchResult>? _snapshot;

    public SearchResultsChannel(IOptions<SeraraConsoleAppOptions> options)
    {
        _maxResults = options.Value.MaxSearchResults;
    }

    public void Reset()
    {
        lock (_lock)
        {
            _results.Clear();
            _snapshot = null;
            while (_available.Reader.TryRead(out _)) { }
        }
    }

    public bool TryPublish(ModuleSearchResult result)
    {
        lock (_lock)
        {
            var path = result.Summary;
            if (_results.ContainsKey(path))
            {
                return true;
            }

            if (_results.Count >= _maxResults)
            {
                return false;
            }

            _results.Add(path, result);
            _snapshot = null;
            _available.Writer.TryWrite(true);
            return true;
        }
    }

    public async Task WaitUntilAvailable(CancellationToken cancellationToken)
    {
        while (true)
        {
            lock (_lock)
            {
                if (_results.Count > 0)
                {
                    return;
                }
            }

            await _available.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public IReadOnlyList<ModuleSearchResult> GetItems()
    {
        lock (_lock)
        {
            return _snapshot ??= _results
                .Values.OrderByDescending(x => x.Summary)
                .ToList()
                .AsReadOnly();
        }
    }
}
