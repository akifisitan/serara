using System.Diagnostics;

namespace Serara.Files;

public static class Utils
{
    private static readonly string _fileEditorPath = GetFileEditorPath();
    private static readonly string _fileExplorerPath = @"C:\Windows\explorer.exe";
    private static readonly string _zipFileEditorPath = @"C:\Program Files\7-Zip\7zFM.exe";

    private static string GetFileEditorPath()
    {
        const string npp64BitPath = @"C:\Program Files\Notepad++\notepad++.exe";
        const string npp32BitPath = @"C:\Program Files (x86)\Notepad++\notepad++.exe";
        const string notePadPath = @"C:\Windows\System32\notepad.exe";

        return File.Exists(npp64BitPath) ? npp64BitPath
            : File.Exists(npp32BitPath) ? npp32BitPath
            : notePadPath;
    }

    public static void OpenWithFileEditor(string filePath)
    {
        Process.Start(
            new ProcessStartInfo
            {
                FileName = _fileEditorPath,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true,
            }
        );
    }

    public static void OpenWithZipFileViewer(string filePath)
    {
        Process.Start(
            new ProcessStartInfo
            {
                FileName = _zipFileEditorPath,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true,
            }
        );
    }

    public static void OpenInFileExplorer(string filePath)
    {
        Process.Start(
            new ProcessStartInfo
            {
                FileName = _fileExplorerPath,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true,
            }
        );
    }
}
