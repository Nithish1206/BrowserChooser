using BrowserChooser.Core.Models;

namespace BrowserChooser.Core.Configuration;

/// <summary>A browser added through settings rather than discovered from the registry.</summary>
public sealed class CustomBrowserOptions
{
    public string Name { get; set; } = "";

    /// <summary>Path to the executable. Environment variables such as %LOCALAPPDATA% are expanded.</summary>
    public string ExecutablePath { get; set; } = "";

    /// <summary>Engine family. Leave empty to detect it from the executable.</summary>
    public BrowserFamily? Family { get; set; }

    /// <summary>Chromium only: the "User Data" folder to read profiles from. Leave empty to detect it.</summary>
    public string? UserDataPath { get; set; }
}
