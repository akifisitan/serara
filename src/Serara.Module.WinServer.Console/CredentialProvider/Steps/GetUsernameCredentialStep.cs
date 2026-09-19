using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class GetUsernameCredentialStep
    : BuilderStateMachineStep<CredentialProviderStateMachineState, Unit>
{
    private readonly WinServerConsoleInput _consoleInput;

    public GetUsernameCredentialStep(WinServerConsoleInput consoleInput)
    {
        _consoleInput = consoleInput;
    }

    protected override async Task<PromptResultRecord<Unit>> ExecuteAsync(
        CredentialProviderStateMachineState input,
        CancellationToken cancellationToken
    )
    {
        var (result, value) = await _consoleInput
            .GetUsername(cancellationToken)
            .ConfigureAwait(false);

        if (result == PromptResult.Cancel)
        {
            return result;
        }

        input.Username = value;

        return result;
    }
}
