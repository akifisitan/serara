using Serara.Tui;
using Spectre.Console;

namespace Serara.Module.WinServer.Console;

internal static class WinServerSearchRequestExtensions
{
    public static string ToConsoleDisplay(this WinServerSearchRequest request)
    {
        return $""""
            [{Colors.LightPink}]SelectedApplicationName:[/][{Colors.Cyan}] {request.SelectedData.Name.EscapeMarkup()}[/]
            [{Colors.LightPink}]SelectedServers:[/][{Colors.Cyan}] {string.Join(
                    ", ",
                    request.SelectedServers
                ).EscapeMarkup()}[/]
            [{Colors.LightPink}]ServerSearchStrategy:[/][{Colors.Cyan}] {request.SearchStrategy.ToDisplay()}[/]
            [{Colors.LightPink}]StartTime:[/][{Colors.Cyan}] {request.StartTime:yyyy-MM-dd HH:mm:ss}[/]
            [{Colors.LightPink}]EndTime:[/][{Colors.Cyan}] {request.EndTime:yyyy-MM-dd HH:mm:ss}[/]
            [{Colors.LightPink}]SearchPattern:[/][{Colors.Cyan}] {request.SearchPattern.EscapeMarkup()}[/]
            """";
    }
}
