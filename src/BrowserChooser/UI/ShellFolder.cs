using System.ComponentModel;
using System.Diagnostics;

namespace BrowserChooser.UI;

internal static class ShellFolder
{
    /// <summary>Opens a folder in File Explorer, creating it first so Explorer doesn't show an error.</summary>
    public static void Open(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            var startInfo = new ProcessStartInfo("explorer.exe") { UseShellExecute = false };
            startInfo.ArgumentList.Add(path);
            using Process? process = Process.Start(startInfo);
        }
        catch (Exception ex) when (ex is Win32Exception or IOException or UnauthorizedAccessException)
        {
            // Opening the folder is a convenience; there's nothing useful to do if it fails.
        }
    }
}
