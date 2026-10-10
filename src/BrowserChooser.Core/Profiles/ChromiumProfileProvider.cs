using System.Text.Json;
using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Exceptions;
using BrowserChooser.Core.Models;
using Microsoft.Extensions.Logging;

namespace BrowserChooser.Core.Profiles;

internal sealed class ChromiumProfileProvider(ILogger<ChromiumProfileProvider> logger) : IProfileProvider
{
    public const string LocalStateFileName = "Local State";

    public BrowserFamily Family => BrowserFamily.Chromium;

    public IReadOnlyList<BrowserProfile> GetProfiles(InstalledBrowser browser)
    {
        ArgumentNullException.ThrowIfNull(browser);
        if (browser.UserDataPath is null)
            return [];

        string path = Path.Combine(browser.UserDataPath, LocalStateFileName);
        if (!File.Exists(path))
        {
            logger.LogDebug("{Browser} has no {File} yet", browser.Name, path);
            return [];
        }

        try
        {
            IReadOnlyList<BrowserProfile> profiles = ChromiumLocalStateParser.Parse(SharedFile.ReadAllText(path));
            logger.LogDebug("Read {ProfileCount} profiles of {Browser}", profiles.Count, browser.Name);
            return profiles;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new ProfileReadException($"Could not read the profiles of {browser.Name} from \"{path}\".", path, ex);
        }
    }
}
