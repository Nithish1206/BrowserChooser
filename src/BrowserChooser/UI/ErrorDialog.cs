using BrowserChooser.Core;

namespace BrowserChooser.UI;

/// <summary>Builds error dialogs: a plain-language message, technical details folded away, and a link to the logs.</summary>
internal static class ErrorDialog
{
    private const string LogsLink = "logs";

    public static TaskDialogPage CreatePage(string heading, string message, Exception? exception, string? logsDirectory)
    {
        var page = new TaskDialogPage
        {
            Caption = AppIdentity.DisplayName,
            Heading = heading,
            Text = message,
            Icon = TaskDialogIcon.Error,
            AllowCancel = true,
        };

        if (exception is not null)
        {
            page.Expander = new TaskDialogExpander(DescribeException(exception))
            {
                CollapsedButtonText = "Show details",
                ExpandedButtonText = "Hide details",
            };
        }

        if (logsDirectory is not null)
        {
            page.EnableLinks = true;
            page.Footnote = new TaskDialogFootnote($"More details are in the <a href=\"{LogsLink}\">log files</a>.");
            page.LinkClicked += (_, e) =>
            {
                if (e.LinkHref == LogsLink)
                    ShellFolder.Open(logsDirectory);
            };
        }

        return page;
    }

    /// <summary>Shown by the global handler for exceptions nothing else caught.</summary>
    public static void ShowUnexpected(Exception exception, string? logsDirectory)
    {
        TaskDialogPage page = CreatePage(
            "Something went wrong",
            "Browser Chooser ran into an unexpected problem and has to close.",
            exception,
            logsDirectory);
        page.Buttons.Add(TaskDialogButton.Close);
        TaskDialog.ShowDialog(page, TaskDialogStartupLocation.CenterScreen);
    }

    /// <summary>Exception types and messages down the inner-exception chain; the stack trace stays in the log.</summary>
    private static string DescribeException(Exception exception)
    {
        var lines = new List<string>();
        for (Exception? current = exception; current is not null; current = current.InnerException)
            lines.Add($"{current.GetType().Name}: {current.Message}");

        return string.Join(Environment.NewLine, lines);
    }
}
