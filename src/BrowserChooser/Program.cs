using BrowserChooser.Core.Commands;
using BrowserChooser.ExceptionHandling;
using BrowserChooser.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BrowserChooser;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // Install the handlers before anything else so that even start-up failures are logged and reported.
        GlobalExceptionHandler.Install();
        ApplicationConfiguration.Initialize();

        AppHost? host = null;
        try
        {
            host = AppHost.Build();
            GlobalExceptionHandler.Attach(host.Logger, host.Environment.LogsDirectory);

            StartupCommand command = CommandLineParser.Parse(args);
            // Log the command type only: the command itself contains the full link.
            host.Logger.LogInformation("Browser Chooser {Version} started ({Command})", Application.ProductVersion, command.GetType().Name);

            ExitCode exitCode = host.Services.GetRequiredService<CommandDispatcher>().Dispatch(command);
            host.Logger.LogInformation("Exiting with {ExitCode}", exitCode.ToString());
            return (int)exitCode;
        }
        catch (Exception ex)
        {
            GlobalExceptionHandler.Report(ex);
            return (int)ExitCode.UnexpectedError;
        }
        finally
        {
            // Disposing the host flushes the log file.
            host?.Dispose();
        }
    }
}
