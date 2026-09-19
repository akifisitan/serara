using Serara.Core.Authentication;

namespace Serara.Module.WinServer.Console;

internal sealed class CredentialProviderStateMachineState : StateMachineState<BasicAuthCredential>
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}
