using Serara.Tui;

namespace Serara.Console;

internal sealed class SearchRunView : IView
{
    public static readonly ViewId Id = new(nameof(SearchRunView));

    private readonly IViewManager _viewManager;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public SearchRunView(IViewManager viewManager, IServiceScopeFactory serviceScopeFactory)
    {
        _viewManager = viewManager;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var serviceScope = _serviceScopeFactory.CreateAsyncScope();

            var serviceProvider = serviceScope.ServiceProvider;

            serviceProvider.GetRequiredService<ViewContext>().ViewId = Id;

            var console = serviceProvider.GetRequiredService<IMultiViewConsole>();

            var searchOptionsMessage = await _viewManager
                .ReadMessageFromView<SearchOptionsMessage>(Id, cancellationToken)
                .ConfigureAwait(false);

            console.Clear();

            _viewManager.NavigateToViewIfActive(MainView.Id, Id);

            var searchRunOrchestrator = serviceProvider.GetRequiredService<SearchRunOrchestrator>();

            var searchRunFinishedEvent = await searchRunOrchestrator
                .RunSearch(searchOptionsMessage.Value, cancellationToken)
                .ConfigureAwait(false);

            _viewManager.SendMessageToView(MainView.Id, searchRunFinishedEvent);

            _viewManager.NavigateToViewIfActive(Id, MainView.Id);
        }
    }
}
