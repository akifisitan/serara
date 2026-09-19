using Serara.Core.Search;
using Serara.Tui;

namespace Serara.Console;

public interface IConsoleInput
{
    Task<ISearchWorkflow> SelectSearchWorkflow(
        IReadOnlyList<ISearchWorkflow> workflows,
        CancellationToken cancellationToken
    );

    Task<IInteractiveAction> SelectInteractiveAction(
        IReadOnlyList<IInteractiveAction> actions,
        CancellationToken cancellationToken
    );

    Task<PromptResultRecord<bool>> GetUserConfirmation(
        string text,
        bool defaultValue = true,
        bool withNavigation = true,
        bool withCancel = false,
        CancellationToken cancellationToken = default
    );

    Task<PromptResultRecord<List<ModuleSearchResult>>> SelectFoundResults(
        Func<IReadOnlyList<ModuleSearchResult>> getChoices,
        CancellationToken cancellationToken
    );
}
