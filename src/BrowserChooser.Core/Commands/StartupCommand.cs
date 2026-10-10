namespace BrowserChooser.Core.Commands;

/// <summary>What the process was started to do, parsed from its command line.</summary>
public abstract record StartupCommand
{
    private StartupCommand()
    {
    }

    /// <summary>Started by Windows for a clicked link: <c>BrowserChooser.exe "https://…"</c>.</summary>
    public sealed record OpenLink(Uri Url) : StartupCommand;

    /// <summary>Started with an argument that is not an http or https link.</summary>
    public sealed record InvalidLink(string Value) : StartupCommand;

    /// <summary><c>--register</c>: add Browser Chooser to Windows' list of browsers.</summary>
    /// <param name="OpenSettings">
    /// Open Default apps settings afterwards; <c>--register --quiet</c> skips this (used by the installer).
    /// </param>
    public sealed record Register(bool OpenSettings = true) : StartupCommand;

    /// <summary><c>--unregister</c>: remove Browser Chooser from Windows' list of browsers.</summary>
    public sealed record Unregister : StartupCommand;

    /// <summary>Started without a link (from the Start menu, or by double-clicking the exe).</summary>
    public sealed record ShowHome : StartupCommand;
}
