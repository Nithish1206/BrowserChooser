namespace BrowserChooser.Core.Discovery;

/// <summary>Reads registry shell commands such as <c>"C:\...\chrome.exe" --single-argument %1</c>.</summary>
internal static class ShellCommandParser
{
    /// <returns>The executable the command runs, or <see langword="null"/> if the command is empty or malformed.</returns>
    public static string? GetExecutablePath(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return null;

        string expanded = Environment.ExpandEnvironmentVariables(command).Trim();

        if (expanded.StartsWith('"'))
        {
            int closingQuote = expanded.IndexOf('"', 1);
            return closingQuote > 1 ? expanded[1..closingQuote] : null;
        }

        // Unquoted paths may still contain spaces (C:\Program Files\...), so cut after ".exe" rather than at a space.
        int exe = expanded.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        return exe > 0 ? expanded[..(exe + ".exe".Length)] : expanded.Split(' ', 2)[0];
    }
}
