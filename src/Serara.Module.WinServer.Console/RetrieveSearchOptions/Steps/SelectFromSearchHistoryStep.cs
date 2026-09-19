using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class SelectFromSearchHistoryStep
    : BuilderStateMachineStep<RetrieveSearchRunOptionsStateMachineRecord, SearchHistoryResolution>
{
    private readonly WinServerConsoleInput _consoleInput;
    private readonly SearchHistory? _searchHistory;
    private readonly MetadataProvider _metadataProvider;

    public SelectFromSearchHistoryStep(
        WinServerConsoleInput consoleInput,
        SearchHistory? searchHistory,
        MetadataProvider metadataProvider
    )
    {
        _consoleInput = consoleInput;
        _searchHistory = searchHistory;
        _metadataProvider = metadataProvider;
    }

    protected override async Task<PromptResultRecord<SearchHistoryResolution>> ExecuteAsync(
        RetrieveSearchRunOptionsStateMachineRecord input,
        CancellationToken cancellationToken
    )
    {
        if (_searchHistory is null)
        {
            return PromptResult.Cancel;
        }

        var (result, historyEntry) = await _consoleInput
            .SelectFromSearchHistoryEntries(_searchHistory.RetrieveAll(), cancellationToken)
            .ConfigureAwait(false);

        if (result == PromptResult.Cancel)
        {
            return result;
        }

        var payload = historyEntry.Payload;
        input.StartTime = payload.StartTime;
        input.EndTime = payload.EndTime;
        input.SearchPattern = payload.SearchPattern;
        input.SearchStrategy = payload.ServerSearchStrategy;

        var metadata = await _metadataProvider.GetMetadata(cancellationToken).ConfigureAwait(false);
        var application = metadata.FirstOrDefault(application =>
            string.Equals(application.Name, payload.ApplicationName, StringComparison.Ordinal)
        );
        if (application is null)
        {
            return new(PromptResult.Success, SearchHistoryResolution.MissingApplication);
        }

        input.SelectedData = application;
        var selectedServers = new List<string>();
        foreach (var serverName in payload.SelectedServers)
        {
            var currentServerName = application.Servers.FirstOrDefault(server =>
                string.Equals(server, serverName, StringComparison.OrdinalIgnoreCase)
            );
            if (currentServerName is null)
            {
                return new(PromptResult.Success, SearchHistoryResolution.MissingServers);
            }

            selectedServers.Add(currentServerName);
        }

        input.SelectedServers = selectedServers;

        return new(result, SearchHistoryResolution.Complete);
    }
}
