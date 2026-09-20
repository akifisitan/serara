using System.Net.Http.Headers;
using System.Text;

namespace Serara.Infra.Confluence;

public sealed class ConfluenceHttpClientFactory : IDisposable
{
    private HttpClient? _cachedHttpClient;

    private string? _cachedKvp;
    private AuthenticationHeaderValue? _authenticationHeader;

    private readonly SocketsHttpHandler _httpHandler;
    private readonly IConfluenceUserCredentialProvider _confluenceUserCredentialProvider;
    private readonly IOptions<ConfluenceHttpClientFactoryOptions> _options;

    public ConfluenceHttpClientFactory(
        IConfluenceUserCredentialProvider confluenceUserCredentialProvider,
        IOptions<ConfluenceHttpClientFactoryOptions> options
    )
    {
        _confluenceUserCredentialProvider = confluenceUserCredentialProvider;
        _options = options;
        _httpHandler = new()
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        };

        if (_options.Value.DisableSslValidation)
        {
            _httpHandler.SslOptions = new()
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true,
            };
        }
    }

    public HttpClient CreateClient()
    {
        if (_cachedHttpClient is not null)
        {
            _cachedHttpClient.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue();

            return _cachedHttpClient;
        }

        _cachedHttpClient = new HttpClient(_httpHandler, disposeHandler: false)
        {
            BaseAddress = new Uri(_options.Value.BaseUrl),
            Timeout = TimeSpan.FromMilliseconds(_options.Value.TimeoutMs),
        };

        _cachedHttpClient.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue();

        return _cachedHttpClient;
    }

    private AuthenticationHeaderValue GetAuthenticationHeaderValue()
    {
        var userCredentials = _confluenceUserCredentialProvider.Get();

        var kvp = $"{userCredentials.Username}:{userCredentials.Password}";

        if (_cachedKvp == kvp)
        {
            return _authenticationHeader!;
        }

        _cachedKvp = kvp;
        _authenticationHeader = new(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(_cachedKvp))
        );

        return _authenticationHeader;
    }

    public void Dispose()
    {
        _cachedHttpClient?.Dispose();
        _httpHandler.Dispose();
    }
}
