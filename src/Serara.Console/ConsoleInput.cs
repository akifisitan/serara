using Serara.Core.Search;
using Serara.Tui;
using Spectre.Console;

namespace Serara.Console;

internal sealed class ConsoleInput : IConsoleInput
{
    private readonly MultiViewConsole _console;
    private readonly IPromptAdapter _promptAdapter;
    private readonly KeyBindingSet _bindings;

    public ConsoleInput(
        MultiViewConsole console,
        IPromptAdapter promptAdapter,
        KeyBindingSet bindings
    )
    {
        _console = console;
        _promptAdapter = promptAdapter;
        _bindings = bindings;
    }

    public Task<ISearchWorkflow> SelectSearchWorkflow(
        IReadOnlyList<ISearchWorkflow> workflows,
        CancellationToken cancellationToken
    ) =>
        SelectCapability(
            "Select search workflow",
            workflows,
            workflow => workflow.DisplayName,
            cancellationToken
        );

    public Task<IInteractiveAction> SelectInteractiveAction(
        IReadOnlyList<IInteractiveAction> actions,
        CancellationToken cancellationToken
    ) =>
        SelectCapability("Select action", actions, action => action.DisplayName, cancellationToken);

    public Task<PromptResultRecord<bool>> GetUserConfirmation(
        string text,
        bool defaultValue = true,
        bool withNavigation = true,
        bool withCancel = false,
        CancellationToken cancellationToken = default
    )
    {
        var prompt = new SelectionPrompt<bool>()
            .Title(text)
            .AddChoices(defaultValue ? [true, false] : [false, true])
            .UseConverter(x => x ? "Yes" : "No")
            .DefaultValue(defaultValue)
            .WrapAround();

        return _console.Prompt(
            () =>
                _promptAdapter.Setup(
                    prompt,
                    withNavigation: withNavigation,
                    withCancel: withCancel
                ),
            cancellationToken
        );
    }

    public Task<PromptResultRecord<List<ModuleSearchResult>>> SelectFoundResults(
        Func<IReadOnlyList<ModuleSearchResult>> getChoices,
        CancellationToken cancellationToken
    )
    {
        return _console.Prompt(
            () =>
            {
                var choices = getChoices();
                if (choices.Count == 0)
                {
                    return new InternalPromptResultRecord<List<ModuleSearchResult>>(
                        InternalPromptResult.Cancel,
                        []
                    );
                }

                var prompt = new MultiSelectionPrompt<ModuleSearchResult>()
                    .Title($"[{Colors.DarkSeaGreen}]Select files to open[/]")
                    .AddChoices(choices)
                    .SearchPlaceholderText($"[{Colors.Gray}](Type to filter): [/]")
                    .WrapAround()
                    .UseSearchFilter(
                        (result, search) =>
                            result.Summary.Contains(search, StringComparison.OrdinalIgnoreCase)
                    )
                    .PageSize(15)
                    .InstructionsText(
                        $"(Press <tab> to select, <alt> + <a> to toggle all, <enter> to accept, <{_bindings[KeyBindingAction.RefreshResults].ToDisplayString().ToLowerInvariant()}> to refresh)"
                    )
                    .UseConverter(result => result.Summary.EscapeMarkup());

                return _promptAdapter.Setup(prompt, withNavigation: true, withRefresh: true);
            },
            cancellationToken
        );
    }

    private Task<T> SelectCapability<T>(
        string title,
        IReadOnlyList<T> choices,
        Func<T, string> display,
        CancellationToken cancellationToken
    )
        where T : notnull
    {
        if (choices.Count == 1)
        {
            return Task.FromResult(choices[0]);
        }

        var prompt = new SelectionPrompt<T>()
            .Title($"[{Colors.DarkSeaGreen}]{title}[/]")
            .AddChoices(choices)
            .UseConverter(choice => display(choice).EscapeMarkup())
            .WrapAround();

        return Select();

        async Task<T> Select()
        {
            var result = await _console
                .Prompt(() => _promptAdapter.Setup(prompt, withNavigation: true), cancellationToken)
                .ConfigureAwait(false);
            return result.Value;
        }
    }
}
