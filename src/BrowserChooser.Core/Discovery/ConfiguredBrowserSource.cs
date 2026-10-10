using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Configuration;
using BrowserChooser.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BrowserChooser.Core.Discovery;

/// <summary>Browsers listed under <c>BrowserChooser:CustomBrowsers</c> in appsettings.json.</summary>
internal sealed class ConfiguredBrowserSource(
    IOptions<BrowserChooserOptions> options,
    BrowserClassifier classifier,
    ILogger<ConfiguredBrowserSource> logger) : IBrowserSource
{
    public IEnumerable<InstalledBrowser> GetBrowsers()
    {
        var browsers = new List<InstalledBrowser>();
        foreach (CustomBrowserOptions custom in options.Value.CustomBrowsers)
        {
            if (string.IsNullOrWhiteSpace(custom.Name) || string.IsNullOrWhiteSpace(custom.ExecutablePath))
            {
                logger.LogWarning("Ignoring a custom browser that has no Name or ExecutablePath");
                continue;
            }

            string executablePath = Environment.ExpandEnvironmentVariables(custom.ExecutablePath);
            if (!File.Exists(executablePath))
            {
                logger.LogWarning("Ignoring custom browser {Browser}: {ExecutablePath} does not exist", custom.Name, executablePath);
                continue;
            }

            InstalledBrowser detected = classifier.Classify("custom:" + custom.Name, custom.Name, executablePath);
            browsers.Add(detected with
            {
                Family = custom.Family ?? detected.Family,
                UserDataPath = string.IsNullOrWhiteSpace(custom.UserDataPath)
                    ? detected.UserDataPath
                    : Environment.ExpandEnvironmentVariables(custom.UserDataPath),
            });
        }

        return browsers;
    }
}
