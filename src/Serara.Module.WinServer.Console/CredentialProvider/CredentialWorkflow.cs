using Serara.Core.Authentication;
using Serara.Tui;

namespace Serara.Module.WinServer.Console;

internal sealed class CredentialWorkflow
{
    private readonly WinServerConsoleInput _input;
    private readonly IMultiViewConsole _console;

    public CredentialWorkflow(WinServerConsoleInput input, IMultiViewConsole console)
    {
        _input = input;
        _console = console;
    }

    public async Task<BasicAuthCredential> Run(CancellationToken cancellationToken)
    {
        while (true)
        {
            var (usernameResult, username) = await _input
                .GetUsername(cancellationToken)
                .ConfigureAwait(false);
            if (usernameResult != PromptResult.Success)
            {
                throw new InvalidOperationException();
            }

            var (passwordResult, password) = await _input
                .GetPassword(cancellationToken)
                .ConfigureAwait(false);
            _console.Clear();
            if (passwordResult == PromptResult.Cancel)
            {
                continue;
            }

            if (passwordResult != PromptResult.Success)
            {
                throw new InvalidOperationException();
            }

            return new(username, password);
        }
    }
}
