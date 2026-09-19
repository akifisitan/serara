using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal abstract class BuilderStateMachineStep<TIn, TOut> : IBuilderStateMachineStep<TIn>
{
    public Func<PromptResultRecord<TOut>, Type> Func { get; set; } = null!;

    public virtual async Task<BuilderStateMachineResult> Execute(
        TIn input,
        CancellationToken cancellationToken
    )
    {
        var result = await ExecuteAsync(input, cancellationToken).ConfigureAwait(false);

        var fn = Func(result);

        return new BuilderStateMachineResult(fn, result.Value);
    }

    protected abstract Task<PromptResultRecord<TOut>> ExecuteAsync(
        TIn input,
        CancellationToken cancellationToken
    );
}

internal interface IBuilderStateMachineStep<TIn>
{
    Task<BuilderStateMachineResult> Execute(TIn input, CancellationToken cancellationToken);
}

internal sealed record BuilderStateMachineResult(Type NextStep, object? Result);
