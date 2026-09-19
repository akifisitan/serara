using Serara.Core.Search;
using Serara.Files;
using Serara.Search;
using Serara.Tui;
using Spectre.Console;

namespace Serara.Console;

internal sealed class OpenLogFileView : IView
{
    public static readonly ViewId Id = new(nameof(OpenLogFileView));

    private readonly ILogger<OpenLogFileView> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IViewManager _viewManager;

    public OpenLogFileView(
        ILogger<OpenLogFileView> logger,
        IServiceScopeFactory serviceScopeFactory,
        IViewManager viewManager
    )
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _viewManager = viewManager;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        using var serviceScope = _serviceScopeFactory.CreateAsyncScope();

        var serviceProvider = serviceScope.ServiceProvider;

        serviceProvider.GetRequiredService<ViewContext>().ViewId = Id;

        var console = serviceProvider.GetRequiredService<MultiViewConsole>();
        var backendRegistry = serviceProvider.GetRequiredService<ISearchBackendRegistry>();

        console.WriteLine($"[{Colors.Title}]Extraction Logs[/]");

        while (!cancellationToken.IsCancellationRequested)
        {
            var selectedResultsMessage = await _viewManager
                .ReadMessageFromView<SearchResultsMessage>(Id, cancellationToken)
                .ConfigureAwait(false);

            await foreach (
                var task in Task.WhenEach(
                        selectedResultsMessage.Value.Select(result =>
                            OpenResultInFileEditor(
                                console,
                                backendRegistry,
                                result,
                                cancellationToken
                            )
                        )
                    )
                    .ConfigureAwait(false)
            )
            {
                if (task.Result is not null)
                {
                    console.WriteLine(
                        $"[{Colors.Success}]Opened file {task.Result.EscapeMarkup()} successfully[/]"
                    );
                }
            }
        }
    }

    private async Task<string?> OpenResultInFileEditor(
        MultiViewConsole console,
        ISearchBackendRegistry backendRegistry,
        ModuleSearchResult result,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var path = await backendRegistry
                .Get(result.Source)
                .Materialize(result.Source, cancellationToken)
                .ConfigureAwait(false);
            Utils.OpenWithFileEditor(path);
            return path;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred while opening {result.Summary}");

            console.WriteLine($"[{Colors.Error}]An error occurred while opening file[/]");

            return null;
        }
    }
}
