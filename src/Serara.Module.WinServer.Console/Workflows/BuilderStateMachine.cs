using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed record BuilderStateMachine<TState, TResult>
    where TState : StateMachineState<TResult>, new()
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<IBuilderStateMachineStep<TState>> _steps = [];

    public BuilderStateMachine(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public BuilderStateMachine<TState, TResult> AddStep<TStep, TOut>(
        Func<PromptResultRecord<TOut>, Type> func
    )
        where TStep : BuilderStateMachineStep<TState, TOut>
    {
        var step = _serviceProvider.GetRequiredService<TStep>();
        step.Func = func;
        _steps.Add(step);
        return this;
    }

    public BuilderStateMachine<TState, TResult> AddStep<TStep>(
        Func<PromptResultRecord<Unit>, Type> func
    )
        where TStep : BuilderStateMachineStep<TState, Unit>
    {
        return AddStep<TStep, Unit>(func);
    }

    public void Reset()
    {
        _steps.Clear();
    }

    public async Task<TResult> Execute(CancellationToken cancellationToken)
    {
        var state = new TState();

        var currentStep = _steps.First();

        var (nextStepType, _) = await currentStep
            .Execute(state, cancellationToken)
            .ConfigureAwait(false);

        while (!state.IsFinished)
        {
            currentStep =
                (IBuilderStateMachineStep<TState>)_serviceProvider.GetRequiredService(nextStepType);

            (nextStepType, _) = await currentStep
                .Execute(state, cancellationToken)
                .ConfigureAwait(false);
        }

        return state.ReturnValue!;
    }
}
