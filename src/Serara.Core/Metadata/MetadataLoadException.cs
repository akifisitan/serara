namespace Serara.Core.Metadata;

public class MetadataLoadException : Exception
{
    public bool IsRetryable { get; }

    public MetadataLoadException(
        string message,
        bool isRetryable = true,
        Exception? innerException = null
    )
        : base(message, innerException)
    {
        IsRetryable = isRetryable;
    }
}
