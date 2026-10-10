namespace BrowserChooser.UI.Chooser;

/// <summary>The picker's fonts at one DPI. Windows 11 uses Segoe UI Variable; older systems fall back to Segoe UI.</summary>
internal sealed class ChooserFonts : IDisposable
{
    public ChooserFonts(int dpi)
    {
        Title = Create(["Segoe UI Variable Display Semib", "Segoe UI Semibold"], 20, dpi);
        Body = Create(["Segoe UI Variable Text", "Segoe UI"], 14, dpi);
        Caption = Create(["Segoe UI Variable Text", "Segoe UI"], 12, dpi);
    }

    /// <summary>20 px semibold, like the system dialog's title.</summary>
    public Font Title { get; }

    /// <summary>14 px, for section headers, list items and buttons.</summary>
    public Font Body { get; }

    /// <summary>12 px, for the link under the title.</summary>
    public Font Caption { get; }

    public void Dispose()
    {
        Title.Dispose();
        Body.Dispose();
        Caption.Dispose();
    }

    private static Font Create(string[] families, float logicalPixels, int dpi)
    {
        float pixels = logicalPixels * dpi / 96f;
        foreach (string family in families)
        {
            var font = new Font(family, pixels, FontStyle.Regular, GraphicsUnit.Pixel);
            // GDI+ silently substitutes a default font for missing families; the name tells us if it did.
            if (font.Name.Equals(family, StringComparison.OrdinalIgnoreCase))
                return font;

            font.Dispose();
        }

        return new Font(SystemFonts.MessageBoxFont?.FontFamily ?? FontFamily.GenericSansSerif, pixels, FontStyle.Regular, GraphicsUnit.Pixel);
    }
}
