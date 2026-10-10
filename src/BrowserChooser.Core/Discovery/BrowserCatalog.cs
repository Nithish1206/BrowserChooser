using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Configuration;
using BrowserChooser.Core.Exceptions;
using BrowserChooser.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BrowserChooser.Core.Discovery;

/// <summary>
/// Merges every <see cref="IBrowserSource"/>, removes duplicates, exclusions and Browser Chooser itself, then
/// expands each browser into one target per profile.
/// </summary>
internal sealed class BrowserCatalog(
    IEnumerable<IBrowserSource> sources,
    IEnumerable<IProfileProvider> profileProviders,
    IOptions<BrowserChooserOptions> options,
    AppEnvironment environment,
    ILogger<BrowserCatalog> logger) : IBrowserCatalog
{
    private readonly Dictionary<BrowserFamily, IProfileProvider> _profileProviders =
        profileProviders.ToDictionary(provider => provider.Family);

    public IReadOnlyList<BrowserTarget> GetTargets()
    {
        List<BrowserTarget> targets = GetBrowsers().SelectMany(GetTargets).ToList();
        logger.LogInformation("Found {TargetCount} choices across {BrowserCount} browsers",
            targets.Count, targets.Select(t => t.Browser.Id).Distinct().Count());
        return targets;
    }

    internal List<InstalledBrowser> GetBrowsers()
    {
        var excluded = new HashSet<string>(options.Value.ExcludedBrowsers, StringComparer.OrdinalIgnoreCase);
        var seenExecutables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var browsers = new List<InstalledBrowser>();

        foreach (InstalledBrowser browser in sources.SelectMany(source => source.GetBrowsers()))
        {
            string executablePath = Path.GetFullPath(browser.ExecutablePath);

            if (IsSelf(browser, executablePath))
                continue;

            if (excluded.Contains(browser.Name) || excluded.Contains(browser.Id) || excluded.Contains(Path.GetFileName(executablePath)))
            {
                logger.LogDebug("Excluding {Browser} by configuration", browser.Name);
                continue;
            }

            // The same browser is often registered in both registry views, or in the registry and in settings.
            if (seenExecutables.Add(executablePath))
                browsers.Add(browser);
        }

        return browsers.OrderBy(browser => browser.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    /// <summary>
    /// Never offer Browser Chooser as a choice: once it is the default browser that would open the picker again,
    /// forever.
    /// </summary>
    private bool IsSelf(InstalledBrowser browser, string executablePath) =>
        browser.Id.Equals(AppIdentity.RegistrationKey, StringComparison.OrdinalIgnoreCase) ||
        Path.GetFileName(executablePath).Equals(Path.GetFileName(environment.ExecutablePath), StringComparison.OrdinalIgnoreCase);

    private IEnumerable<BrowserTarget> GetTargets(InstalledBrowser browser)
    {
        IReadOnlyList<BrowserProfile> profiles = [];
        if (_profileProviders.TryGetValue(browser.Family, out IProfileProvider? provider))
        {
            try
            {
                profiles = provider.GetProfiles(browser);
            }
            catch (ProfileReadException ex)
            {
                // Still offer the browser itself; it will open in its last-used profile.
                logger.LogWarning(ex, "Could not read profiles of {Browser}; offering it without a profile", browser.Name);
            }
        }

        return profiles.Count == 0
            ? [new BrowserTarget(browser, Profile: null)]
            : profiles.Select(profile => new BrowserTarget(browser, profile));
    }
}
