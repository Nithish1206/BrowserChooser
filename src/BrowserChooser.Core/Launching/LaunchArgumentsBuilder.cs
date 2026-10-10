using BrowserChooser.Core.Models;

namespace BrowserChooser.Core.Launching;

/// <summary>Builds the command-line arguments that open a link in a given browser profile.</summary>
internal static class LaunchArgumentsBuilder
{
    public static IReadOnlyList<string> Build(BrowserTarget target, Uri url)
    {
        // AbsoluteUri is percent-encoded, so it never contains spaces or quotes that could split it into
        // several arguments, and it always starts with "http", so it can't be mistaken for a switch.
        string link = url.AbsoluteUri;

        return (target.Browser.Family, target.Profile) switch
        {
            (BrowserFamily.Chromium, { } profile) => [$"--profile-directory={profile.Id}", link],
            (BrowserFamily.Firefox, { } profile) => ["-P", profile.Id, link],
            _ => [link],
        };
    }
}
