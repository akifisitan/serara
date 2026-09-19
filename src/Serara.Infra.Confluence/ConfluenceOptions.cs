using FluentValidation;
using Serara.Core.Authentication;

namespace Serara.Infra.Confluence;

public sealed record ConfluenceUserCredential(string Username, string Password)
    : BasicAuthCredential(Username, Password);

public sealed record ConfluenceHttpClientFactoryOptions
{
    public required string BaseUrl { get; set; }
    public required int TimeoutMs { get; set; }
}

public sealed class ConfluenceHttpClientFactoryOptionsValidator
    : AbstractValidator<ConfluenceHttpClientFactoryOptions>
{
    public ConfluenceHttpClientFactoryOptionsValidator()
    {
        RuleFor(x => x.BaseUrl).NotEmpty();
        RuleFor(x => x.TimeoutMs).InclusiveBetween(1 * 1000, 20 * 1000);
    }
}
