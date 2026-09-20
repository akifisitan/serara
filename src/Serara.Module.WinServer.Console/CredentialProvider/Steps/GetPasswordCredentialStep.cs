using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class GetPasswordCredentialStep
    : BuilderStateMachineStep<CredentialProviderStateMachineState, Unit>
{
    private readonly WinServerConsoleInput _consoleInput;
    private readonly IMultiViewConsole _console;

    public GetPasswordCredentialStep(WinServerConsoleInput consoleInput, IMultiViewConsole console)
    {
        _consoleInput = consoleInput;
        _console = console;
    }

    protected override async Task<PromptResultRecord<Unit>> ExecuteAsync(
        CredentialProviderStateMachineState input,
        CancellationToken cancellationToken
    )
    {
        var (result, value) = await _consoleInput
            .GetPassword(cancellationToken)
            .ConfigureAwait(false);

        _console.Clear();

        if (result == PromptResult.Cancel)
        {
            return result;
        }

        input.Password = value;

        input.Finish(new(input.Username, input.Password));

        return result;
    }
}
