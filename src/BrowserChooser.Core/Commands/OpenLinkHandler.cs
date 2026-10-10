using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Exceptions;
using BrowserChooser.Core.Models;
using BrowserChooser.Core.Preferences;
using Microsoft.Extensions.Logging;

namespace BrowserChooser.Core.Commands;

/// <summary>The main use case: a link was clicked, so ask which browser to use and open it there.</summary>
public sealed class OpenLinkHandler(
    IBrowserCatalog catalog,
    IBrowserPicker picker,
    IBrowserLauncher launcher,
    IPreferencesStore preferencesStore,
    IUserNotifier notifier,
    ILogger<OpenLinkHandler> logger)
{
    public ExitCode Handle(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);
        logger.LogInformation("Choosing a browser for {Link}", LinkParser.ToLogString(url));

        IReadOnlyList<BrowserTarget> targets = catalog.GetTargets();
        if (targets.Count == 0)
        {
            logger.LogWarning("No browsers were found");
            notifier.ShowError(
                "No browsers found",
                "Browser Chooser couldn't find any installed browsers. Portable browsers can be added under " +
                "\"CustomBrowsers\" in appsettings.json.");
            return ExitCode.NoBrowsersFound;
        }

        UserPreferences preferences = preferencesStore.Load();
        BrowserTarget? lastUsed = preferences.LastUsed?.FindIn(targets);

        // If a launch fails, let the user pick another browser so the link isn't lost.
        while (true)
        {
            BrowserTarget? target = picker.Pick(url, targets, lastUsed);
            if (target is null)
            {
                logger.LogInformation("The user cancelled");
                return ExitCode.Cancelled;
            }

            try
            {
                launcher.Launch(target, url);
            }
            catch (BrowserLaunchException ex)
            {
                logger.LogError(ex, "Could not open {Target}", target.DisplayName);
                if (!notifier.AskToChooseAgain($"Couldn't open {target.DisplayName}", ex.Message, ex))
                    return ExitCode.LaunchFailed;

                continue;
            }

            preferencesStore.Save(preferences with { LastUsed = TargetReference.From(target) });
            return ExitCode.Success;
        }
    }
}
