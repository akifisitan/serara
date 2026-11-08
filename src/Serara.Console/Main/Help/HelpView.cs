using Serara.Tui;
using Spectre.Console;

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

        string Shortcut(KeyBindingAction action, string description)
        {
            var key = _bindings[action].ToDisplayString();
            return $"  [cyan]{key.PadRight(24).EscapeMarkup()}[/]{description.EscapeMarkup()}";
        }

        console.WriteLine(
            $"""

            [bold]Navigation[/]
            {Shortcut(KeyBindingAction.HelpView, Presentation.Title)}
            {Shortcut(KeyBindingAction.MainView, MainView.Presentation.Title)}
            {Shortcut(KeyBindingAction.FileExplorerView, FileExplorerView.Presentation.Title)}
            {Shortcut(KeyBindingAction.OpenLogView, OpenLogFileView.Presentation.Title)}
            {Shortcut(KeyBindingAction.PreviousView, "Previous view")}
            {Shortcut(KeyBindingAction.NextView, "Next view")}

            [bold]Context-specific actions[/]
            {Shortcut(KeyBindingAction.Cancel, "Cancel / back, where supported")}
            {Shortcut(KeyBindingAction.RefreshResults, "Refresh search results")}
            {Shortcut(KeyBindingAction.OpenInExplorer, "Open in Explorer, in file browsing")}
            """
        );
    }
}
