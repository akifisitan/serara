using Serara.Tui;

namespace Serara.Console;

internal sealed class HelpView : IView
{
    public static readonly ViewId Id = new(nameof(HelpView));
    public static readonly ViewPresentation Presentation = new("Help", Colors.Gray);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly KeyBindingSet _bindings;

    public HelpView(IServiceScopeFactory serviceScopeFactory, KeyBindingSet bindings)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _bindings = bindings;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        using var serviceScope = _serviceScopeFactory.CreateAsyncScope();

        var serviceProvider = serviceScope.ServiceProvider;

        serviceProvider.GetRequiredService<ViewContext>().ViewId = Id;

        var console = serviceProvider.GetRequiredService<IMultiViewConsole>();

        console.WriteLine(
            $"""
            {_bindings[KeyBindingAction.HelpView].ToDisplayString()} -> {Presentation.Title}
            {_bindings[
                KeyBindingAction.MainView
            ].ToDisplayString()} -> {MainView.Presentation.Title}
            {_bindings[
                KeyBindingAction.FileExplorerView
            ].ToDisplayString()} -> {FileExplorerView.Presentation.Title}
            {_bindings[
                KeyBindingAction.OpenLogView
            ].ToDisplayString()} -> {OpenLogFileView.Presentation.Title}
            {_bindings[KeyBindingAction.PreviousView].ToDisplayString()} -> Previous View
            {_bindings[KeyBindingAction.NextView].ToDisplayString()} -> Next View
            {_bindings[KeyBindingAction.Cancel].ToDisplayString()} -> Cancel / Back
            {_bindings[KeyBindingAction.RefreshResults].ToDisplayString()} -> Refresh Results
            {_bindings[KeyBindingAction.OpenInExplorer].ToDisplayString()} -> Open in Explorer
            """
        );
    }
}
