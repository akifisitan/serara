namespace Serara.Hosting;

internal sealed class ApplicationFailure
{
    public bool HasFailed { get; private set; }
    public Exception? Exception { get; private set; }

    public void MarkFailed(Exception? exception = null)
    {
        HasFailed = true;
        Exception = exception;
    }
}
