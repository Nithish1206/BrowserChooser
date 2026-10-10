using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Exceptions;
using BrowserChooser.Core.Models;
using Microsoft.Extensions.Logging;

namespace BrowserChooser.Core.Profiles;

/// <summary>Firefox keeps its profile list in the roaming profile: <c>%APPDATA%\Mozilla\Firefox\profiles.ini</c>.</summary>
internal sealed class FirefoxProfileProvider(AppEnvironment environment, ILogger<FirefoxProfileProvider> logger)
    : IProfileProvider
{
    public BrowserFamily Family => BrowserFamily.Firefox;

    public IReadOnlyList<BrowserProfile> GetProfiles(InstalledBrowser browser)
    {
        ArgumentNullException.ThrowIfNull(browser);

        string path = Path.Combine(environment.RoamingAppData, "Mozilla", "Firefox", "profiles.ini");
        if (!File.Exists(path))
        {
            logger.LogDebug("{Browser} has no {File} yet", browser.Name, path);
            return [];
        }

        try
        {
            IReadOnlyList<BrowserProfile> profiles = FirefoxProfilesIniParser.Parse(SharedFile.ReadAllText(path));
            logger.LogDebug("Read {ProfileCount} profiles of {Browser}", profiles.Count, browser.Name);
            return profiles;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ProfileReadException($"Could not read the profiles of {browser.Name} from \"{path}\".", path, ex);
        }
    }
}
