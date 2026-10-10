namespace BrowserChooser.UI;

internal static class DisplayText
{
    public static string Shorten(string text, int maxLength) =>
        text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength - 1), "…");

    /// <summary>Doubles "&amp;" so Windows doesn't turn it into a keyboard-shortcut underline.</summary>
    public static string EscapeMnemonic(string text) => text.Replace("&", "&&", StringComparison.Ordinal);
}
