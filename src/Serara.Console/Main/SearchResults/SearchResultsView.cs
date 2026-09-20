using Serara.Tui;
using Spectre.Console;

namespace Serara.Console;

internal sealed class SearchResultsView : IView
{
    public static readonly ViewId Id = new(nameof(SearchResultsView));

    private readonly ILogger<SearchResultsView> _logger;
    private readonly SearchResultsChannel _resultsChannel;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IViewManager _viewManager;
    private readonly OpenLogFileView _openLogFileView;

    public SearchResultsView(
        ILogger<SearchResultsView> logger,
        SearchResultsChannel resultsChannel,
        IServiceScopeFactory serviceScopeFactory,
        IViewManager viewManager,
        OpenLogFileView openLogFileView
    )
    {
        _logger = logger;
        _resultsChannel = resultsChannel;
        _serviceScopeFactory = serviceScopeFactory;
        _viewManager = viewManager;
        _openLogFileView = openLogFileView;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        using var serviceScope = _serviceScopeFactory.CreateAsyncScope();

        var serviceProvider = serviceScope.ServiceProvider;

        serviceProvider.GetRequiredService<ViewContext>().ViewId = Id;

        var consoleInput = serviceProvider.GetRequiredService<IConsoleInput>();
        var console = serviceProvider.GetRequiredService<IMultiViewConsole>();

        _viewManager.RegisterView(OpenLogFileView.Id);

        var openLogFileViewTask = _openLogFileView.Start(cancellationToken);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_resultsChannel.GetItems().Count == 0)
                {
                    console.WriteLine($"[{Colors.Title}]Results will show up here when found[/]");
                    await _resultsChannel
                        .WaitUntilAvailable(cancellationToken)
                        .ConfigureAwait(false);
                    console.Clear();
                }

                var (result, selectedResults) = await consoleInput
                    .SelectFoundResults(_resultsChannel.GetItems, cancellationToken)
                    .ConfigureAwait(false);

                if (result != PromptResult.Success)
                {
                    continue;
                }

                var currentResults = _resultsChannel.GetItems().ToHashSet();
                selectedResults.RemoveAll(x => !currentResults.Contains(x));
                if (selectedResults.Count == 0)
                {
                    continue;
                }

                _logger.ZLogInformation(
                    $"Selected files:{Environment.NewLine}{(selectedResults.Count == 0 ? "None"
                : string.Join(Environment.NewLine, selectedResults.Select(x => x.Summary)))}"
                );

                _viewManager.SendMessageToView(
                    OpenLogFileView.Id,
                    new SearchResultsMessage(selectedResults)
                );

                _viewManager.NavigateToViewIfActive(Id, OpenLogFileView.Id);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            try
            {
                await openLogFileViewTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }
    }
}
