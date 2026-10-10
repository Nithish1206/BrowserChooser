using BrowserChooser.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BrowserChooser.ExceptionHandling;

/// <summary>
/// The last line of defence. Expected failures (<see cref="Core.Exceptions.BrowserChooserException"/>) are handled
/// where they happen; anything reaching this class is a bug, so it is logged as critical and the user gets one
/// error dialog instead of a crash.
/// </summary>
internal static class GlobalExceptionHandler
{
    private static ILogger s_logger = NullLogger.Instance;
    private static string? s_logsDirectory;
    private static int s_dialogShown;

    /// <summary>Subscribes to every source of unhandled exceptions. Call before creating any window.</summary>
    public static void Install()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Report(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Report(e.ExceptionObject as Exception ?? new InvalidOperationException($"Non-exception thrown: {e.ExceptionObject}"));
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            s_logger.LogError(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };
    }

    /// <summary>Starts writing reports to the real logger once it has been created.</summary>
    public static void Attach(ILogger logger, string logsDirectory)
    {
        s_logger = logger;
        s_logsDirectory = logsDirectory;
    }

    public static void Report(Exception exception)
    {
        s_logger.LogCritical(exception, "Unhandled exception");

        // Several threads can fail at once; one dialog is enough.
        if (Interlocked.Exchange(ref s_dialogShown, 1) == 1)
            return;

        try
        {
            ErrorDialog.ShowUnexpected(exception, s_logsDirectory);
        }
        catch (Exception dialogException) when (dialogException is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // No UI is possible (for example, no interactive desktop). The log entry above has to do.
            s_logger.LogError(dialogException, "Could not show the error dialog");
        }
    }
}
