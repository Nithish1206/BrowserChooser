namespace BrowserChooser.UI;

/// <summary>
/// The application icon, embedded in the assembly with all its sizes. Unlike extracting it from the exe, which
/// gives a single 32 px image, this lets Windows pick a crisp size for the title bar, taskbar and Alt+Tab.
/// </summary>
internal static class AppIcon
{
    private const string ResourceName = "BrowserChooser.ico";

    /// <summary>The icon at the size closest to <paramref name="size"/>, or <see langword="null"/> if it is missing.</summary>
    /// <remarks>The caller owns and disposes the returned icon.</remarks>
    public static Icon? Create(int size)
    {
        using Stream? stream = typeof(AppIcon).Assembly.GetManifestResourceStream(ResourceName);
        return stream is null ? null : new Icon(stream, size, size);
    }

    /// <summary>The icon at the standard size for the current display scale.</summary>
    public static Icon? CreateDefault() => Create(SystemInformation.IconSize.Width);
}
