using BrowserChooser.Core.Profiles;

namespace BrowserChooser.Core.Discovery;

/// <summary>
/// Finds a Chromium browser's "User Data" folder from its executable. Chromium browsers install to
/// <c>{Program Files | %LOCALAPPDATA%}\Vendor\Product\Application\browser.exe</c> and keep profiles in
/// <c>%LOCALAPPDATA%\Vendor\Product\User Data</c>. That covers Edge, Chrome, Brave and Vivaldi, including
/// their beta, dev and canary channels.
/// </summary>
internal static class ChromiumUserDataLocator
{
    /// <returns>The "User Data" folder if it contains a "Local State" file, otherwise <see langword="null"/>.</returns>
    public static string? Locate(string executablePath, AppEnvironment environment, Func<string, bool> fileExists)
    {
        string? installDirectory = Path.GetDirectoryName(executablePath);
        if (installDirectory is not null &&
            Path.GetFileName(installDirectory).Equals("Application", StringComparison.OrdinalIgnoreCase))
        {
            installDirectory = Path.GetDirectoryName(installDirectory);
        }

        if (string.IsNullOrEmpty(installDirectory))
            return null;

        foreach (string root in new[] { environment.ProgramFiles, environment.ProgramFilesX86, environment.LocalAppData })
        {
            string? relative = GetPathUnder(root, installDirectory);
            if (relative is null)
                continue;

            string userData = Path.Combine(environment.LocalAppData, relative, "User Data");
            if (fileExists(Path.Combine(userData, ChromiumProfileProvider.LocalStateFileName)))
                return userData;
        }

        return null;
    }

    private static string? GetPathUnder(string root, string path)
    {
        if (string.IsNullOrEmpty(root))
            return null;

        // The trailing separator stops "C:\Program Files" from matching "C:\Program Files (x86)\...".
        string prefix = Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar;
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && path.Length > prefix.Length
            ? path[prefix.Length..]
            : null;
    }
}
