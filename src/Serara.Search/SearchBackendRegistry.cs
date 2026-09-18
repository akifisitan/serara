using Serara.Core.Search;

namespace Serara.Search;

public interface ISearchBackendRegistry
{
    ISearchBackend Get(ISearchRequest request);
    ISearchBackend Get(SearchSourceReference source);
}

internal sealed class SearchBackendRegistry : ISearchBackendRegistry
{
    private readonly IReadOnlyList<ISearchBackend> _backends;

    public SearchBackendRegistry(IEnumerable<ISearchBackend> backends)
    {
        _backends = backends.ToList();
    }

    public ISearchBackend Get(ISearchRequest request) =>
        GetSingle(
            request.GetType(),
            backend => backend.RequestType.IsInstanceOfType(request),
            "request"
        );

    public ISearchBackend Get(SearchSourceReference source) =>
        GetSingle(
            source.GetType(),
            backend => backend.SourceType.IsInstanceOfType(source),
            "source"
        );

    private ISearchBackend GetSingle(
        Type valueType,
        Func<ISearchBackend, bool> isMatch,
        string valueName
    )
    {
        var matches = _backends.Where(isMatch).ToList();
        if (matches.Count == 0)
        {
            throw new NotSupportedException(
                $"No search backend is registered for {valueName} type '{valueType}'."
            );
        }

        if (matches.Count > 1)
        {
            throw new InvalidOperationException(
                $"Multiple search backends are registered for {valueName} type '{valueType}': "
                    + $"{string.Join(", ", matches.Select(backend => backend.GetType()))}."
            );
        }

        return matches[0];
    }
}
