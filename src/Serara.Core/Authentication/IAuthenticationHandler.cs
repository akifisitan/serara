namespace Serara.Core.Authentication;

public interface IAuthenticationHandler<TCredential>
{
    Task<TCredential> RequestCredentials(CancellationToken cancellationToken);
}
