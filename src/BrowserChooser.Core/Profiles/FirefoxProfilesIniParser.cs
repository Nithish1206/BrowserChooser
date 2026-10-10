using BrowserChooser.Core.Models;

namespace BrowserChooser.Core.Profiles;

/// <summary>
/// Reads profile names from Firefox's profiles.ini. Each profile is a <c>[ProfileN]</c> section with a
/// <c>Name=</c> key; <c>[General]</c> and <c>[Install…]</c> sections are ignored.
/// </summary>
internal static class FirefoxProfilesIniParser
{
    public static IReadOnlyList<BrowserProfile> Parse(string content)
    {
        var profiles = new List<BrowserProfile>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? section = null;
        string? name = null;

        void AddCurrentProfile()
        {
            bool isProfileSection = section?.StartsWith("Profile", StringComparison.OrdinalIgnoreCase) == true;
            if (isProfileSection && !string.IsNullOrWhiteSpace(name) && seenNames.Add(name))
                profiles.Add(new BrowserProfile(name, name));
        }

        foreach (string rawLine in content.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] is ';' or '#')
                continue;

            if (line[0] == '[' && line[^1] == ']')
            {
                AddCurrentProfile();
                section = line[1..^1].Trim();
                name = null;
                continue;
            }

            int separator = line.IndexOf('=', StringComparison.Ordinal);
            if (separator > 0 && line[..separator].Trim().Equals("Name", StringComparison.OrdinalIgnoreCase))
                name = line[(separator + 1)..].Trim();
        }

        AddCurrentProfile();
        return profiles;
    }
}
