using Serara.Console;

namespace Serara.Module.WinServer.Console;

internal sealed class WinServerSearchWorkflow : SearchWorkflow<WinServerSearchRequest>
{
    private readonly SearchOptionsWorkflow _options;
    private readonly SearchHistory? _history;
    private readonly ILogger<WinServerSearchWorkflow> _logger;

    public WinServerSearchWorkflow(
        SearchOptionsWorkflow options,
        SearchHistory? history,
        ILogger<WinServerSearchWorkflow> logger
    )
    {
        _options = options;
        _history = history;
        _logger = logger;
    }

    public override string Id => "win-server-search";
    public override string DisplayName => "Windows server logs";

    protected override async Task<SearchWorkflowSelection<WinServerSearchRequest>> RetrieveSearch(
        CancellationToken cancellationToken
    )
    {
        var searchOptions = await _options.Run(cancellationToken).ConfigureAwait(false);
        if (_history is not null)
        {
            try
            {
                await _history.Add(searchOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"Failed to save search history");
            }
        }

        return new SearchWorkflowSelection<WinServerSearchRequest>(
            searchOptions,
            searchOptions.ToConsoleDisplay()
        );
    }
}
