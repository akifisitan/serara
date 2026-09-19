namespace Serara.Module.WinServer.Console;

internal static class SearchStrategyExtensions
{
    public static string ToDisplay(this WinServerSearchStrategy value)
    {
        return value switch
        {
            WinServerSearchStrategy.LiveAndArchiveServers => "Search in live and archive servers",
            WinServerSearchStrategy.LiveServersOnly => "Search in live servers only",
            WinServerSearchStrategy.ArchiveServersOnly => "Search in archive servers only",
            _ => throw new NotSupportedException(),
        };
    }
}
