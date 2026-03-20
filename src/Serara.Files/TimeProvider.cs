namespace Serara.Files;

public interface ITimeProvider
{
    DateTimeOffset LocalNow();
    DateTimeOffset UtcNow();
}

internal sealed class TimeProvider : ITimeProvider
{
    public DateTimeOffset LocalNow() => DateTimeOffset.Now;

    public DateTimeOffset UtcNow() => DateTimeOffset.UtcNow;
}
