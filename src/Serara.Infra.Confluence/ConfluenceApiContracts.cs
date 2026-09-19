namespace Serara.Infra.Confluence;

public sealed record SearchResponse(string Id, string Type, string Title, SearchResponseBody Body);

public sealed record SearchResponseBody(SearchResponseBodyView View);

public sealed record SearchResponseBodyView(string Value);
