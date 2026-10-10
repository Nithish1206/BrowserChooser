using System.Security;
using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace BrowserChooser.Core.Discovery;

/// <summary>
/// Reads the browsers registered with Windows under <c>SOFTWARE\Clients\StartMenuInternet</c>, the same list
/// Settings › Default apps shows. Per-user installs (Brave, Chrome without admin rights) register under HKCU.
/// </summary>
internal sealed class RegistryBrowserSource(BrowserClassifier classifier, ILogger<RegistryBrowserSource> logger)
    : IBrowserSource
{
    private const string StartMenuInternetPath = @"SOFTWARE\Clients\StartMenuInternet";

    private static readonly (RegistryHive Hive, RegistryView View)[] Locations =
    [
        (RegistryHive.CurrentUser, RegistryView.Default),
        (RegistryHive.LocalMachine, RegistryView.Registry64),
        (RegistryHive.LocalMachine, RegistryView.Registry32),
    ];

    public IEnumerable<InstalledBrowser> GetBrowsers()
    {
        var browsers = new List<InstalledBrowser>();
        foreach ((RegistryHive hive, RegistryView view) in Locations)
        {
            try
            {
                using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view);
                using RegistryKey? clients = baseKey.OpenSubKey(StartMenuInternetPath);
                if (clients is null)
                    continue;

                foreach (string keyName in clients.GetSubKeyNames())
                {
                    InstalledBrowser? browser = TryReadBrowser(clients, keyName);
                    if (browser is not null)
                        browsers.Add(browser);
                }
            }
            catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
            {
                logger.LogWarning(ex, "Could not read registered browsers from {Hive} ({View})", hive, view);
            }
        }

        return browsers;
    }

    private InstalledBrowser? TryReadBrowser(RegistryKey clients, string keyName)
    {
        try
        {
            using RegistryKey? key = clients.OpenSubKey(keyName);
            using RegistryKey? command = key?.OpenSubKey(@"shell\open\command");

            string? executablePath = ShellCommandParser.GetExecutablePath(command?.GetValue(null) as string);
            if (executablePath is null || !File.Exists(executablePath))
            {
                logger.LogDebug("Skipping registered browser {RegistryKey}: executable {ExecutablePath} not found", keyName, executablePath);
                return null;
            }

            // Values starting with '@' are indirect resource strings ("@C:\...\file.dll,-123"); the key name reads better.
            string? name = key?.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(name) || name.StartsWith('@'))
                name = keyName;

            return classifier.Classify(keyName, name.Trim(), executablePath);
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            logger.LogWarning(ex, "Could not read registered browser {RegistryKey}", keyName);
            return null;
        }
    }
}
