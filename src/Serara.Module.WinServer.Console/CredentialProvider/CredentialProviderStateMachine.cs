using Serara.Core.Authentication;
using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class CredentialProviderStateMachine
{
    private readonly BuilderStateMachine<
        CredentialProviderStateMachineState,
        BasicAuthCredential
    > _stateMachine;

    public CredentialProviderStateMachine(
        BuilderStateMachine<CredentialProviderStateMachineState, BasicAuthCredential> stateMachine
    )
    {
        _stateMachine = stateMachine;
    }

    public async Task<BasicAuthCredential> RunAsync(CancellationToken cancellationToken)
    {
        _stateMachine.Reset();

        return await _stateMachine
            .AddStep<GetUsernameCredentialStep>(x =>
                x switch
                {
                    (PromptResult.Success, _) => typeof(GetPasswordCredentialStep),
                    _ => throw new InvalidOperationException(),
                }
            )
            .AddStep<GetPasswordCredentialStep>(x =>
                x switch
                {
                    (PromptResult.Success, _) => typeof(Unit),
                    (PromptResult.Cancel, _) => typeof(GetUsernameCredentialStep),
                    _ => throw new InvalidOperationException(),
                }
            )
            .Execute(cancellationToken)
            .ConfigureAwait(false);
    }
}
