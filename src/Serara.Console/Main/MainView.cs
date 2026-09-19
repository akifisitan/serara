using Serara.History;
using Serara.Tui;
using Spectre.Console;

namespace Serara.Console;

internal sealed class MainView : IView
{
    public static readonly ViewId Id = new(nameof(MainView));

    private readonly IViewManager _viewManager;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<MainView> _logger;

    private readonly SearchRunView _searchRunView;
    private readonly SearchResultsView _resultsView;
    private readonly FileExplorerView _fileExplorerView;
    private readonly HelpView _helpView;

    public MainView(
        IViewManager viewManager,
        IServiceScopeFactory serviceScopeFactory,
        SearchRunView searchRunView,
        SearchResultsView resultsView,
        ILogger<MainView> logger,
        FileExplorerView fileExplorerView,
        HelpView helpView
    )
    {
        _viewManager = viewManager;
        _serviceScopeFactory = serviceScopeFactory;
        _searchRunView = searchRunView;
        _resultsView = resultsView;
        _logger = logger;
        _fileExplorerView = fileExplorerView;
        _helpView = helpView;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        _viewManager.RegisterView(Id);

        using var serviceScope = _serviceScopeFactory.CreateAsyncScope();

        var serviceProvider = serviceScope.ServiceProvider;

        serviceProvider.GetRequiredService<ViewContext>().ViewId = Id;

        using var viewCancellationTokenSource = new CancellationTokenSource();

        _viewManager.RegisterView(HelpView.Id);
        var helpViewTask = _helpView.Start(viewCancellationTokenSource.Token);

        _viewManager.RegisterView(FileExplorerView.Id);
        var fileExplorerViewTask = _fileExplorerView.Start(viewCancellationTokenSource.Token);

        var console = serviceProvider.GetRequiredService<MultiViewConsole>();
        var consoleInput = serviceProvider.GetRequiredService<IConsoleInput>();
        var searchWorkflows = serviceProvider.GetServices<ISearchWorkflow>().ToList();
        var historyStore = serviceProvider.GetService<ISearchHistoryStore>();

        if (historyStore is not null)
        {
            try
            {
                await HandleUnavailableHistory(
                        console,
                        consoleInput,
                        historyStore,
                        searchWorkflows,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"Failed to initialize search history");
            }
        }

        var searchOptions = await RetrieveSearch(consoleInput, searchWorkflows)
            .ConfigureAwait(false);

        _viewManager.RegisterView(SearchRunView.Id);
        _viewManager.RegisterView(SearchResultsView.Id);

        var searchRunViewTask = _searchRunView.Start(viewCancellationTokenSource.Token);
        var resultsViewTask = _resultsView.Start(viewCancellationTokenSource.Token);

        await StartSearchRun(console, searchOptions).ConfigureAwait(false);

        while (
            (
                await consoleInput
                    .GetUserConfirmation(
                        $"[{Colors.Ask}]Run another query?[/]",
                        cancellationToken: CancellationToken.None
                    )
                    .ConfigureAwait(false)
            ).Value
        )
        {
            console.Clear();

            searchOptions = await RetrieveSearch(consoleInput, searchWorkflows)
                .ConfigureAwait(false);

            await StartSearchRun(console, searchOptions).ConfigureAwait(false);
        }

        await viewCancellationTokenSource.CancelAsync().ConfigureAwait(false);

        await foreach (
            var task in Task.WhenEach(
                    helpViewTask,
                    fileExplorerViewTask,
                    resultsViewTask,
                    searchRunViewTask
                )
                .ConfigureAwait(false)
        )
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An unexpected error occurred");
            }
        }
    }

    private static async Task<SearchWorkflowSelection> RetrieveSearch(
        IConsoleInput consoleInput,
        IReadOnlyList<ISearchWorkflow> workflows
    )
    {
        var workflow = await consoleInput
            .SelectSearchWorkflow(workflows, CancellationToken.None)
            .ConfigureAwait(false);
        return await workflow.RetrieveSearch(CancellationToken.None).ConfigureAwait(false);
    }

    private static async Task HandleUnavailableHistory(
        MultiViewConsole console,
        IConsoleInput consoleInput,
        ISearchHistoryStore historyStore,
        IReadOnlyList<ISearchWorkflow> workflows,
        CancellationToken cancellationToken
    )
    {
        await historyStore.EnsureInitialized(cancellationToken).ConfigureAwait(false);
        var availableCapabilityIds = workflows
            .Select(workflow => workflow.Id)
            .ToHashSet(StringComparer.Ordinal);
        var unavailableEntries = historyStore
            .RetrieveAll()
            .Where(entry => !availableCapabilityIds.Contains(entry.CapabilityId))
            .OrderByDescending(entry => entry.CreatedAt)
            .ToList();

        foreach (var entry in unavailableEntries)
        {
            console.WriteLine(
                $"[{Colors.Gray}]History entry '{entry.Name.EscapeMarkup()}' is unavailable because capability '{entry.CapabilityId.EscapeMarkup()}' is not installed.[/]"
            );
            var delete = await consoleInput
                .GetUserConfirmation(
                    $"[{Colors.Ask}]Delete this unavailable history entry?[/]",
                    defaultValue: false,
                    withNavigation: false,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
            if (delete.Value)
            {
                await historyStore.Remove(entry.Id, cancellationToken).ConfigureAwait(false);
            }
        }

        if (unavailableEntries.Count > 0)
        {
            console.Clear();
        }
    }

    private async Task StartSearchRun(
        MultiViewConsole console,
        SearchWorkflowSelection searchOptions
    )
    {
        _viewManager.SendMessageToView(
            SearchRunView.Id,
            new SearchOptionsMessage(searchOptions.Request)
        );

        console.Clear();
        console.WriteLine($"[{Colors.Title}]Started search run with the following parameters:[/]");
        console.WriteLine(searchOptions.Display);
        console.WriteLine($"[{Colors.Gray}]Search run ongoing...[/]");

        var searchRunFinishedEvent = await _viewManager
            .ReadMessageFromView<SearchRunFinishedEvent>(Id, CancellationToken.None)
            .ConfigureAwait(false);

        console.Clear();
        console.WriteLine($"[{Colors.Title}]Started search run with the following parameters:[/]");
        console.WriteLine(searchOptions.Display);
        console.WriteLine(
            searchRunFinishedEvent.IsCancelled ? $"[{Colors.Error}]Search run cancelled[/]"
            : searchRunFinishedEvent.FailureCount > 0
                ? $"[{Colors.Yellow}]Search run finished with errors. Results may be incomplete.[/]"
            : $"[{Colors.Success}]Search run complete[/]"
        );

        if (searchRunFinishedEvent.FailureCount > 0)
        {
            console.WriteLine(
                $"[{Colors.Error}]{searchRunFinishedEvent.FailureCount} search operation(s) failed. See the search logs for details.[/]"
            );
        }

        if (searchRunFinishedEvent.ResultLimitReached)
        {
            console.WriteLine(
                $"[{Colors.Yellow}]The result limit was reached. Additional matching files are recorded in the logs but are not listed in the results view.[/]"
            );
        }
    }
}
