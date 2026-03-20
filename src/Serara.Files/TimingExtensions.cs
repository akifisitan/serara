using System.Diagnostics;

namespace Serara.Files;

public static class TimingExtensions
{
    public static double GetElapsedTotalMs(this long timestamp)
    {
        return Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
    }

    public static string ToReadableTime(this double ms)
    {
        const long Second = 1000;
        const long Minute = Second * 60;
        const long Hour = Minute * 60;
        const long Day = Hour * 24;

        return ms switch
        {
            < Second => $"{ms:F2} ms",
            < Minute => $"{ms / Second:F2} s",
            < Hour => $"{ms / Minute:F2} m",
            < Day => $"{ms / Hour:F2} h",
            _ => $"{ms / Day:F2} d",
        };
    }

    public static string ToReadableTime(this long timestamp)
    {
        const long Second = 1000;
        const long Minute = Second * 60;
        const long Hour = Minute * 60;
        const long Day = Hour * 24;

        var ms = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;

        return ms switch
        {
            < Second => $"{ms:F2} ms",
            < Minute => $"{ms / Second:F2} s",
            < Hour => $"{ms / Minute:F2} m",
            < Day => $"{ms / Hour:F2} h",
            _ => $"{ms / Day:F2} d",
        };
    }

    public static string ToReadableFileSize(this long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        return bytes switch
        {
            < KB => $"{bytes} B",
            < MB => $"{(double)bytes / KB:F2} KB",
            < GB => $"{(double)bytes / MB:F2} MB",
            _ => $"{(double)bytes / GB:F2} GB",
        };
    }
}
