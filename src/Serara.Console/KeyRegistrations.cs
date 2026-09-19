using Serara.Tui;

namespace Serara.Console;

internal sealed class KeyRegistrations
{
    public KeyRegistrations(IKeyRegistrationMapping keyRegistrationMapping, KeyBindingSet bindings)
    {
        Mapping = keyRegistrationMapping;
        MoveNextPress = new(nameof(MoveNextPress), keyRegistrationMapping.MoveNextPressed);
        MovePrevPress = new(nameof(MovePrevPress), keyRegistrationMapping.MovePrevPressed);
        CancelPress = Create(nameof(CancelPress), bindings[KeyBindingAction.Cancel]);
        RefreshListPress = Create(
            nameof(RefreshListPress),
            bindings[KeyBindingAction.RefreshResults]
        );
        OpenInExplorerPress = Create(
            nameof(OpenInExplorerPress),
            bindings[KeyBindingAction.OpenInExplorer]
        );
    }

    public CustomKeyPress MoveNextPress { get; }
    public CustomKeyPress MovePrevPress { get; }
    public CustomKeyPress CancelPress { get; }
    public CustomKeyPress RefreshListPress { get; }
    public CustomKeyPress OpenInExplorerPress { get; }

    public IKeyRegistrationMapping Mapping { get; }

    private static CustomKeyPress Create(string name, KeyChord chord) => new(name, chord.Matches);
}

internal sealed class KeyRegistrationMapping : IKeyRegistrationMapping
{
    private readonly KeyChord _moveNext;
    private readonly KeyChord _movePrevious;

    public KeyRegistrationMapping(KeyBindingSet bindings)
    {
        _moveNext = bindings[KeyBindingAction.NextView];
        _movePrevious = bindings[KeyBindingAction.PreviousView];
        NavigationMapping =
        [
            Create(MainView.Id, nameof(MainView), bindings[KeyBindingAction.MainView]),
            Create(
                FileExplorerView.Id,
                nameof(FileExplorerView),
                bindings[KeyBindingAction.FileExplorerView]
            ),
            Create(
                OpenLogFileView.Id,
                nameof(OpenLogFileView),
                bindings[KeyBindingAction.OpenLogView]
            ),
            Create(HelpView.Id, nameof(HelpView), bindings[KeyBindingAction.HelpView]),
        ];
    }

    public IReadOnlyList<(ViewId ViewId, CustomKeyPress CustomKeyPress)> NavigationMapping { get; }

    public bool MoveNextPressed(ConsoleKeyInfo x) => _moveNext.Matches(x);

    public bool MovePrevPressed(ConsoleKeyInfo x) => _movePrevious.Matches(x);

    private static (ViewId, CustomKeyPress) Create(ViewId viewId, string name, KeyChord chord) =>
        (viewId, new CustomKeyPress(name, chord.Matches));
}
