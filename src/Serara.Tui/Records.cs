namespace Serara.Tui;

public static class Colors
{
    public const string Success = Green;
    public const string Error = Red;
    public const string Warning = Yellow;
    public const string Ask = Cyan;
    public const string Title = DarkSeaGreen;
    public const string Log = Gray;

    public const string Blue = "blue";
    public const string Green = "green";
    public const string Yellow = "yellow";
    public const string DarkOrange = "darkorange";
    public const string DarkOrange3 = "darkorange3";
    public const string DarkOrange3_1 = "darkorange3";
    public const string Red = "red";
    public const string Gray = "grey";
    public const string Cornsilk = "cornsilk1";
    public const string Cyan = "cyan";
    public const string DarkBlue = "darkblue";
    public const string DarkRed = "darkred";
    public const string DarkSeaGreen = "darkseagreen";
    public const string LightPink = "lightpink1";
    public const string Teal = "teal";
}

public sealed class ViewContext : IViewContext
{
    public ViewId ViewId
    {
        get =>
            field
            ?? throw new InvalidOperationException($"Must set {nameof(ViewId)} before accessing");
        set =>
            field =
                value
                ?? throw new InvalidOperationException(
                    $"{nameof(ViewContext)} has already been initialized"
                );
    }
}

public interface IViewContext
{
    ViewId ViewId { get; }
}
