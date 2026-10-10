using BrowserChooser.Core;
using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Commands;
using BrowserChooser.Core.Registration;

namespace BrowserChooser.UI;

/// <summary>
/// Shown when Browser Chooser is started without a link: explains the set-up and offers the actions to complete it.
/// </summary>
internal sealed class HomeDialog(RegistrationHandler registration, IUserNotifier notifier, AppEnvironment environment)
{
    public ExitCode Show()
    {
        RegistrationStatus status = registration.GetStatus();

        var register = new TaskDialogCommandLinkButton(
            status == RegistrationStatus.NotRegistered ? "Register as a browser" : "Register again",
            "Adds Browser Chooser to the list of browsers in Windows, for your account only. No admin rights are needed.");
        var openSettings = new TaskDialogCommandLinkButton(
            "Open Default apps settings",
            "Make Browser Chooser the default for HTTP and HTTPS so it opens every link you click.");
        var unregister = new TaskDialogCommandLinkButton(
            "Unregister",
            "Removes Browser Chooser from the list of browsers in Windows.");
        var openLogs = new TaskDialogCommandLinkButton("Open log folder", environment.LogsDirectory);

        // The task dialog also uses this icon for its title bar and taskbar button.
        using Icon? appIcon = AppIcon.CreateDefault();
        var page = new TaskDialogPage
        {
            Caption = AppIdentity.DisplayName,
            Heading = AppIdentity.DisplayName,
            Text = DescribeStatus(status),
            Icon = appIcon is null ? TaskDialogIcon.Information : new TaskDialogIcon(appIcon),
            AllowCancel = true,
        };
        page.Buttons.Add(register);
        if (status != RegistrationStatus.NotRegistered)
        {
            page.Buttons.Add(openSettings);
            page.Buttons.Add(unregister);
        }
        page.Buttons.Add(openLogs);
        page.Buttons.Add(TaskDialogButton.Close);
        page.DefaultButton = status == RegistrationStatus.Registered ? openSettings : register;

        TaskDialogButton clicked = TaskDialog.ShowDialog(page, TaskDialogStartupLocation.CenterScreen);

        if (clicked == register)
            return registration.Register();

        if (clicked == openSettings)
        {
            registration.OpenDefaultAppsSettings();
        }
        else if (clicked == unregister)
        {
            ExitCode result = registration.Unregister();
            if (result == ExitCode.Success)
                notifier.ShowInformation("Unregistered", "Browser Chooser has been removed from the list of browsers in Windows.");
            return result;
        }
        else if (clicked == openLogs)
        {
            ShellFolder.Open(environment.LogsDirectory);
        }

        return ExitCode.Success;
    }

    private static string DescribeStatus(RegistrationStatus status) => status switch
    {
        RegistrationStatus.Registered =>
            "Browser Chooser is registered. If links still open straight in a browser, make Browser Chooser " +
            "the default for HTTP and HTTPS in Default apps.",
        RegistrationStatus.RegisteredElsewhere =>
            "Browser Chooser is registered from a different folder, so links go to that copy. Register again " +
            "to use this one.",
        _ =>
            "Browser Chooser asks which browser and profile to use every time you click a link. To set it " +
            "up, register it as a browser, then make it the default for HTTP and HTTPS in Default apps.",
    };
}
