using System.Diagnostics.CodeAnalysis;

namespace BrowserChooser.Core;

/// <summary>Validates the links Browser Chooser is asked to open. Only absolute http and https URLs are accepted.</summary>
public static class LinkParser
{
    public static bool TryParse(string? value, [NotNullWhen(true)] out Uri? url)
    {
        url = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string candidate = value.Trim().Trim('"');
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out Uri? parsed) || string.IsNullOrEmpty(parsed.Host))
            return false;

        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
            return false;

        url = parsed;
        return true;
    }

    /// <summary>
    /// Scheme and host only. Paths and query strings often carry secrets (password-reset and sign-in links),
    /// so full links are never written to the log.
    /// </summary>
    public static string ToLogString(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);
        return $"{url.Scheme}://{url.IdnHost}";
    }
}
