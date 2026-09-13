using Spectre.Console;

namespace Serara.Tui;

public interface IPromptAdapter
{
    InternalPromptResultRecord<T> Setup<T>(
        TextPrompt<T> prompt,
        IReadOnlyList<ICustomPromptEvent>? customEvents = null,
        bool withCancel = false,
        bool withNavigation = false
    )
        where T : notnull;

    InternalPromptResultRecord<List<T>> Setup<T>(
        MultiSelectionPrompt<T> prompt,
        IReadOnlyList<ICustomPromptEvent>? customEvents = null,
        bool withCancel = false,
        bool withNavigation = false,
        bool withRefresh = false
    )
        where T : notnull;

    InternalPromptResultRecord<T> Setup<T>(
        SelectionPrompt<T> prompt,
        IReadOnlyList<ICustomPromptEvent>? customEvents = null,
        bool withCancel = false,
        bool withNavigation = false,
        bool withRefresh = false
    )
        where T : notnull;
}
