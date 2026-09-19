namespace Serara.Infra.Confluence;

public sealed class ConfluenceUserCredentialProvider : IConfluenceUserCredentialProvider
{
    private ConfluenceUserCredential? _confluenceUserCredential;

    public void Set(ConfluenceUserCredential confluenceUserCredential)
    {
        _confluenceUserCredential = confluenceUserCredential;
    }

    public ConfluenceUserCredential Get()
    {
        if (_confluenceUserCredential is null)
        {
            throw new ConfluenceError(
                $"{nameof(ConfluenceUserCredential)} not set. Call {nameof(Set)}()"
            );
        }

        return _confluenceUserCredential;
    }
}
