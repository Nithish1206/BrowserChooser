using BrowserChooser.Core;
using BrowserChooser.Core.Abstractions;

namespace BrowserChooser.UI;

internal sealed class TaskDialogUserNotifier(AppEnvironment environment) : IUserNotifier
{
    public void ShowInformation(string heading, string message)
    {
        var page = new TaskDialogPage
        {
            Caption = AppIdentity.DisplayName,
            Heading = heading,
            Text = message,
            Icon = TaskDialogIcon.Information,
            AllowCancel = true,
            Buttons = { TaskDialogButton.OK },
        };
        TaskDialog.ShowDialog(page, TaskDialogStartupLocation.CenterScreen);
    }

    public void ShowError(string heading, string message, Exception? exception = null)
    {
        TaskDialogPage page = ErrorDialog.CreatePage(heading, message, exception, environment.LogsDirectory);
        page.Buttons.Add(TaskDialogButton.Close);
        TaskDialog.ShowDialog(page, TaskDialogStartupLocation.CenterScreen);
    }

    public bool AskToChooseAgain(string heading, string message, Exception? exception = null)
    {
        TaskDialogPage page = ErrorDialog.CreatePage(heading, message, exception, environment.LogsDirectory);
        var chooseAgain = new TaskDialogButton("Choose another browser");
        page.Buttons.Add(chooseAgain);
        page.Buttons.Add(TaskDialogButton.Cancel);
        page.DefaultButton = chooseAgain;

        return TaskDialog.ShowDialog(page, TaskDialogStartupLocation.CenterScreen) == chooseAgain;
    }
}
