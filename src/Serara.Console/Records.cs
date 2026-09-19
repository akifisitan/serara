using Serara.Core.Search;
using Serara.Tui;

namespace Serara.Console;

internal sealed record SearchRunFinishedEvent(
    bool IsCancelled,
    int FailureCount,
    bool ResultLimitReached
) : IViewMessage;

internal sealed record SearchOptionsMessage(ISearchRequest Value) : IViewMessage;

internal sealed record SearchResultsMessage(List<ModuleSearchResult> Value) : IViewMessage;
