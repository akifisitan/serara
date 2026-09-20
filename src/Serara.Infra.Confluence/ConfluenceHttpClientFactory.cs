using System.Net.Http.Headers;
using System.Text;

namespace Serara.Infra.Confluence;

public interface IConfluenceHttpClientFactory
{
    HttpClient CreateClient();
}

internal sealed class ConfluenceHttpClientFactory : IConfluenceHttpClientFactory
{
    public const string ClientName = "Confluence";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfluenceUserCredentialProvider _confluenceUserCredentialProvider;

    public ConfluenceHttpClientFactory(
        IHttpClientFactory httpClientFactory,
        IConfluenceUserCredentialProvider confluenceUserCredentialProvider
    )
    {
        _httpClientFactory = httpClientFactory;
        _confluenceUserCredentialProvider = confluenceUserCredentialProvider;
    }

    public HttpClient CreateClient()
    {
        var (username, password) = _confluenceUserCredentialProvider.Get();
        var encodedCredentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{username}:{password}")
        );

        var httpClient = _httpClientFactory.CreateClient(ClientName);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            encodedCredentials
        );

        return httpClient;
    }
}
