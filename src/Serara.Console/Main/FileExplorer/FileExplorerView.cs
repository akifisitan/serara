using Serara.Tui;

namespace Serara.Console;

internal sealed class FileExplorerView : IView
{
    public static readonly ViewId Id = new(nameof(FileExplorerView));

    private readonly IServiceScopeFactory _serviceScopeFactory;

    public FileExplorerView(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        using var serviceScope = _serviceScopeFactory.CreateAsyncScope();

        var serviceProvider = serviceScope.ServiceProvider;

        serviceProvider.GetRequiredService<ViewContext>().ViewId = Id;

        var console = serviceProvider.GetRequiredService<IMultiViewConsole>();
        var consoleInput = serviceProvider.GetRequiredService<IConsoleInput>();
        var actions = serviceProvider.GetServices<IInteractiveAction>().ToList();
        var action = await consoleInput
            .SelectInteractiveAction(actions, cancellationToken)
            .ConfigureAwait(false);

        await action.Execute(cancellationToken).ConfigureAwait(false);
    }
}
