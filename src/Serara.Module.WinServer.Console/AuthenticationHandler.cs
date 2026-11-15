using Serara.Core.Authentication;

namespace Serara.Module.WinServer.Console;

internal sealed class AuthenticationHandler : IAuthenticationHandler<BasicAuthCredential>
{
    private readonly CredentialWorkflow _credentials;

    public AuthenticationHandler(CredentialWorkflow credentials)
    {
        _credentials = credentials;
    }

    public async Task<BasicAuthCredential> RequestCredentials(CancellationToken cancellationToken)
    {
        var credentials = await _credentials.Run(cancellationToken).ConfigureAwait(false);

        return credentials;
    }
}
