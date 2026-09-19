using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class RetrieveSearchOptionsStateMachine
{
    private readonly BuilderStateMachine<
        RetrieveSearchRunOptionsStateMachineRecord,
        WinServerSearchRequest
    > _stateMachine;

    public RetrieveSearchOptionsStateMachine(
        BuilderStateMachine<
            RetrieveSearchRunOptionsStateMachineRecord,
            WinServerSearchRequest
        > stateMachine
    )
    {
        _stateMachine = stateMachine;
    }

    public async Task<WinServerSearchRequest> RunAsync(CancellationToken cancellationToken)
    {
        _stateMachine.Reset();

        return await _stateMachine
            .AddStep<GetHistoryOrRunChoiceStep, SearchOptionChoice>(x =>
                x switch
                {
                    (PromptResult.Success, SearchOptionChoice.FromHistory) =>
                        typeof(SelectFromSearchHistoryStep),
                    (PromptResult.Success, SearchOptionChoice.NewRun) => typeof(GetApplicationStep),
                    _ => throw new InvalidOperationException(),
                }
            )
            .AddStep<SelectFromSearchHistoryStep, SearchHistoryResolution>(x =>
                x switch
                {
                    (PromptResult.Success, SearchHistoryResolution.Complete) =>
                        typeof(AcceptAndStartStep),
                    (PromptResult.Success, SearchHistoryResolution.MissingApplication) =>
                        typeof(GetApplicationStep),
                    (PromptResult.Success, SearchHistoryResolution.MissingServers) =>
                        typeof(GetServersToSearchStep),
                    (PromptResult.Cancel, _) => typeof(GetHistoryOrRunChoiceStep),
                    _ => throw new InvalidOperationException(),
                }
            )
            .AddStep<AcceptAndStartStep, SearchRequestResolution>(x =>
                x switch
                {
                    (PromptResult.Success, SearchRequestResolution.Complete) => typeof(Unit),
                    (PromptResult.Success, SearchRequestResolution.InvalidApplication) =>
                        typeof(GetApplicationStep),
                    (PromptResult.Success, SearchRequestResolution.InvalidServers) =>
                        typeof(GetServersToSearchStep),
                    (PromptResult.Success, SearchRequestResolution.InvalidSearchStrategy) =>
                        typeof(GetServerSearchStrategyStep),
                    (PromptResult.Success, SearchRequestResolution.InvalidTimeRange) =>
                        typeof(GetSearchTimesStep),
                    (PromptResult.Success, SearchRequestResolution.InvalidSearchPattern) =>
                        typeof(GetSearchPatternStep),
                    (PromptResult.Cancel, SearchRequestResolution.RejectedHistory) =>
                        typeof(GetApplicationStep),
                    (PromptResult.Cancel, SearchRequestResolution.RejectedNewRun) =>
                        typeof(GetSearchPatternStep),
                    _ => throw new InvalidOperationException(),
                }
            )
            .AddStep<GetApplicationStep>(x =>
                x switch
                {
                    (PromptResult.Success, _) => typeof(GetServersToSearchStep),
                    (PromptResult.Cancel, _) => typeof(GetHistoryOrRunChoiceStep),
                    _ => throw new InvalidOperationException(),
                }
            )
            .AddStep<GetServersToSearchStep>(x =>
                x switch
                {
                    (PromptResult.Success, _) => typeof(GetServerSearchStrategyStep),
                    (PromptResult.Cancel, _) => typeof(GetApplicationStep),
                    _ => throw new InvalidOperationException(),
                }
            )
            .AddStep<GetServerSearchStrategyStep>(x =>
                x switch
                {
                    (PromptResult.Success, _) => typeof(GetSearchTimesStep),
                    (PromptResult.Cancel, _) => typeof(GetServersToSearchStep),
                    _ => throw new InvalidOperationException(),
                }
            )
            .AddStep<GetSearchTimesStep>(x =>
                x switch
                {
                    (PromptResult.Success, _) => typeof(GetSearchPatternStep),
                    (PromptResult.Cancel, _) => typeof(GetServerSearchStrategyStep),
                    _ => throw new InvalidOperationException(),
                }
            )
            .AddStep<GetSearchPatternStep>(x =>
                x switch
                {
                    (PromptResult.Success, _) => typeof(AcceptAndStartStep),
                    (PromptResult.Cancel, _) => typeof(GetSearchTimesStep),
                    _ => throw new InvalidOperationException(),
                }
            )
            .Execute(cancellationToken)
            .ConfigureAwait(false);
    }
}
