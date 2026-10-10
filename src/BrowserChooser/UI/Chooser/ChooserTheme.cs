using Microsoft.Win32;

namespace BrowserChooser.UI.Chooser;

/// <summary>
/// Colours of the Windows 11 app picker. The dark values were measured from the system dialog; they are the
/// Fluent design tokens (for example "subtle fill" = white at 6% over the background), so light mode uses the same
/// tokens over light backgrounds.
/// </summary>
internal sealed record ChooserTheme(
    bool IsDark,
    Color Background,
    Color FooterBackground,
    Color FooterDivider,
    Color WindowBorder,
    Color TextPrimary,
    Color TextSecondary,
    Color RowHover,
    Color RowSelected,
    Color RowPressed,
    Color Accent,
    ButtonColors StandardButton,
    Color ScrollThumb,
    Color FocusStroke)
{
    // Indexes into the AccentPalette registry value: 0-2 lighter shades, 3 the accent itself, 4-6 darker shades.
    private const int DarkModeAccentIndex = 1;
    private const int LightModeAccentIndex = 4;

    public static ChooserTheme For(ThemeMode mode)
    {
        bool isDark = mode switch
        {
            ThemeMode.Dark => true,
            ThemeMode.Light => false,
            _ => IsDarkModeEnabled(),
        };
        Color? accent = ReadAccentShade(isDark ? DarkModeAccentIndex : LightModeAccentIndex);
        return isDark
            ? CreateDark(accent ?? Color.FromArgb(0x4C, 0xC2, 0xFF))
            : CreateLight(accent ?? Color.FromArgb(0x00, 0x67, 0xC0));
    }

    private static ChooserTheme CreateDark(Color accent)
    {
        Color background = Color.FromArgb(0x32, 0x32, 0x32);
        Color footer = Color.FromArgb(0x2B, 0x2B, 0x2B);
        Color white = Color.White;
        return new ChooserTheme(
            IsDark: true,
            Background: background,
            FooterBackground: footer,
            FooterDivider: Color.FromArgb(0x27, 0x27, 0x27),
            WindowBorder: Color.FromArgb(0x3C, 0x3C, 0x3C),
            TextPrimary: white,
            TextSecondary: Blend(background, white, 0.786),
            RowHover: Blend(background, white, 0.042),
            RowSelected: Blend(background, white, 0.061),
            RowPressed: Blend(background, white, 0.033),
            Accent: accent,
            StandardButton: new ButtonColors(
                Fill: Blend(footer, white, 0.061),
                FillHover: Blend(footer, white, 0.084),
                FillPressed: Blend(footer, white, 0.033),
                BorderTop: Blend(footer, white, 0.093),
                BorderBottom: Blend(footer, white, 0.070),
                Text: white),
            ScrollThumb: Blend(background, white, 0.544),
            FocusStroke: white);
    }

    private static ChooserTheme CreateLight(Color accent)
    {
        Color background = Color.FromArgb(0xFB, 0xFB, 0xFB);
        Color footer = Color.FromArgb(0xF3, 0xF3, 0xF3);
        Color black = Color.Black;
        return new ChooserTheme(
            IsDark: false,
            Background: background,
            FooterBackground: footer,
            FooterDivider: Color.FromArgb(0xE5, 0xE5, 0xE5),
            WindowBorder: Color.FromArgb(0xD5, 0xD5, 0xD5),
            TextPrimary: Blend(background, black, 0.896),
            TextSecondary: Blend(background, black, 0.606),
            RowHover: Blend(background, black, 0.024),
            RowSelected: Blend(background, black, 0.037),
            RowPressed: Blend(background, black, 0.018),
            Accent: accent,
            StandardButton: new ButtonColors(
                Fill: Color.FromArgb(0xFE, 0xFE, 0xFE),
                FillHover: Color.FromArgb(0xF9, 0xF9, 0xF9),
                FillPressed: Color.FromArgb(0xF5, 0xF5, 0xF5),
                BorderTop: Blend(footer, black, 0.058),
                BorderBottom: Blend(footer, black, 0.162),
                Text: Blend(background, black, 0.896)),
            ScrollThumb: Blend(background, black, 0.446),
            FocusStroke: Blend(background, black, 0.896));
    }

    /// <summary>The colour of <paramref name="overlay"/> at <paramref name="opacity"/> on top of <paramref name="background"/>.</summary>
    private static Color Blend(Color background, Color overlay, double opacity) => Color.FromArgb(
        (int)Math.Round(background.R + ((overlay.R - background.R) * opacity)),
        (int)Math.Round(background.G + ((overlay.G - background.G) * opacity)),
        (int)Math.Round(background.B + ((overlay.B - background.B) * opacity)));

    private static bool IsDarkModeEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int useLightTheme && useLightTheme == 0;
    }

    private static Color? ReadAccentShade(int index)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent");
        if (key?.GetValue("AccentPalette") is not byte[] palette || palette.Length < (index + 1) * 4)
            return null;

        int offset = index * 4;
        return Color.FromArgb(palette[offset], palette[offset + 1], palette[offset + 2]);
    }
}

/// <summary>The colours of one button style in each of its states.</summary>
internal sealed record ButtonColors(Color Fill, Color FillHover, Color FillPressed, Color BorderTop, Color BorderBottom, Color Text);
