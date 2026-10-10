using System.ComponentModel;
using System.Diagnostics;
using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Exceptions;
using BrowserChooser.Core.Registration;
using Microsoft.Extensions.Logging;

namespace BrowserChooser.Core.Commands;

/// <summary>Registers or unregisters Browser Chooser as a browser and sends the user to Default apps settings.</summary>
public sealed class RegistrationHandler(
    IAppRegistrar registrar,
    IProcessStarter processStarter,
    IUserNotifier notifier,
    AppEnvironment environment,
    ILogger<RegistrationHandler> logger)
{
    /// <summary>Opens Browser Chooser's own page in Settings › Apps › Default apps.</summary>
    public const string DefaultAppsSettingsUri = "ms-settings:defaultapps?registeredAppUser=" + AppIdentity.RegistrationKey;

    public RegistrationStatus GetStatus() => registrar.GetStatus(environment.ExecutablePath);

    public ExitCode Register(bool openSettings = true)
    {
        try
        {
            registrar.Register(environment.ExecutablePath);
        }
        catch (AppRegistrationException ex)
        {
            logger.LogError(ex, "Registration failed");
            notifier.ShowError("Couldn't register Browser Chooser", ex.Message, ex);
            return ExitCode.RegistrationFailed;
        }

        if (openSettings)
            OpenDefaultAppsSettings();

        return ExitCode.Success;
    }

    public ExitCode Unregister()
    {
        try
        {
            registrar.Unregister();
            return ExitCode.Success;
        }
        catch (AppRegistrationException ex)
        {
            logger.LogError(ex, "Unregistration failed");
            notifier.ShowError("Couldn't unregister Browser Chooser", ex.Message, ex);
            return ExitCode.RegistrationFailed;
        }
    }

    public void OpenDefaultAppsSettings()
    {
        try
        {
            processStarter.Start(new ProcessStartInfo(DefaultAppsSettingsUri) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            logger.LogWarning(ex, "Could not open Default apps settings");
            notifier.ShowInformation(
                "Set Browser Chooser as your default browser",
                "Open Settings › Apps › Default apps, select Browser Chooser, and choose it for HTTP and HTTPS.");
        }
    }
}
