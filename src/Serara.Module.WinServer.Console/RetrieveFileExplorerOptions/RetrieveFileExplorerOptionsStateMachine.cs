using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class RetrieveFileExplorerOptionsStateMachine
{
    private readonly BuilderStateMachine<
        RetrieveFileExplorerOptionsStateMachineState,
        Unit
    > _stateMachine;

    public RetrieveFileExplorerOptionsStateMachine(
        BuilderStateMachine<RetrieveFileExplorerOptionsStateMachineState, Unit> stateMachine
    )
    {
        _stateMachine = stateMachine;
    }

    public async Task<Unit> RunAsync(CancellationToken cancellationToken)
    {
        _stateMachine.Reset();

        return await _stateMachine
            .AddStep<GetApplicationForFileExplorerStep>(x =>
                x switch
                {
                    (PromptResult.Success, _) => typeof(GetServerForFileExplorerStep),
                    (PromptResult.Cancel, _) => typeof(GetApplicationForFileExplorerStep),
                    _ => throw new InvalidOperationException(),
                }
            )
            .AddStep<GetServerForFileExplorerStep, bool>(x =>
                x switch
                {
                    (PromptResult.Success, true) =>
                        typeof(GetLiveOrArchiveChoiceForFileExplorerStep), // if archive server is available
                    (PromptResult.Success, false) => typeof(SelectDirectoryForFileExplorerStep),
                    (PromptResult.Cancel, _) => typeof(GetApplicationForFileExplorerStep),
                    _ => throw new InvalidOperationException(),
                }
            )
            .AddStep<GetLiveOrArchiveChoiceForFileExplorerStep>(x =>
                x switch
                {
                    (PromptResult.Success, _) => typeof(SelectDirectoryForFileExplorerStep),
                    (PromptResult.Cancel, _) => typeof(GetServerForFileExplorerStep),
                    _ => throw new InvalidOperationException(),
                }
            )
            .AddStep<SelectDirectoryForFileExplorerStep, bool>(x =>
                x switch
                {
                    (PromptResult.Success, _) => typeof(SelectDirectoryForFileExplorerStep),
                    (PromptResult.Cancel, true) =>
                        typeof(GetLiveOrArchiveChoiceForFileExplorerStep),
                    (PromptResult.Cancel, false) => typeof(GetServerForFileExplorerStep),
                    _ => throw new InvalidOperationException(),
                }
            )
            .Execute(cancellationToken)
            .ConfigureAwait(false);
    }
}
