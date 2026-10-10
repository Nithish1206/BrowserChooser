namespace BrowserChooser.Core.Configuration;

/// <summary>The "BrowserChooser" section of appsettings.json.</summary>
public sealed class BrowserChooserOptions
{
    public const string SectionName = "BrowserChooser";

    /// <summary>
    /// Browsers to hide from the picker, matched case-insensitively against the display name
    /// ("Internet Explorer"), the executable file name ("iexplore.exe") or the registry key name.
    /// </summary>
    public List<string> ExcludedBrowsers { get; set; } = [];

    /// <summary>Browsers Windows does not know about, such as portable installs.</summary>
    public List<CustomBrowserOptions> CustomBrowsers { get; set; } = [];
}
