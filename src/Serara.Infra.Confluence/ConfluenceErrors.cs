using Serara.Core.Metadata;

namespace Serara.Infra.Confluence;

public class ConfluenceError(string Message, bool IsRetryable = true)
    : MetadataLoadException(Message, IsRetryable);

public sealed class ConfluenceInvalidCredentialError() : ConfluenceError("Invalid credentials");

public sealed class ConfluenceUnauthorizedError()
    : ConfluenceError("You do not have access to this page", IsRetryable: false);

public sealed class ConfluencePageNotFoundError()
    : ConfluenceError("Page not found. Verify the page id", IsRetryable: false);

public sealed class ConfluenceInvalidPageFormatError()
    : ConfluenceError(
        "Invalid page format, check page format or try another page id",
        IsRetryable: false
    );

public sealed class ConfluenceTimeoutException()
    : ConfluenceError("The request timed out, try again");
