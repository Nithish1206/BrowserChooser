namespace BrowserChooser.UI;

/// <summary>The "Appearance" section of appsettings.json.</summary>
internal sealed class AppearanceOptions
{
    public const string SectionName = "Appearance";

    /// <summary>Colours of the browser picker.</summary>
    public ThemeMode Theme { get; set; } = ThemeMode.System;
}

internal enum ThemeMode
{
    /// <summary>Follow the Windows app mode (Settings › Personalization › Colours).</summary>
    System = 0,
    Light,
    Dark,
}
