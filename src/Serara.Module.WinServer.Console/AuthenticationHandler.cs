using Serara.Core.Authentication;

namespace Serara.Module.WinServer.Console;

internal sealed class AuthenticationHandler : IAuthenticationHandler<BasicAuthCredential>
{
    private readonly CredentialProviderStateMachine _credentialProviderStateMachine;

    public AuthenticationHandler(CredentialProviderStateMachine credentialProviderStateMachine)
    {
        _credentialProviderStateMachine = credentialProviderStateMachine;
    }

    public async Task<BasicAuthCredential> RequestCredentials(CancellationToken cancellationToken)
    {
        var credentials = await _credentialProviderStateMachine
            .RunAsync(cancellationToken)
            .ConfigureAwait(false);

        return credentials;
    }
}
