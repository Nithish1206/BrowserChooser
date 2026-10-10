using System.Globalization;
using BrowserChooser.Hosting;
using Serilog;

namespace BrowserChooser.Logging;

/// <summary>Daily rolling log files in %LOCALAPPDATA%\BrowserChooser\Logs.</summary>
internal static class FileLogging
{
    private const string FileNamePattern = "browserchooser-.log";

    private const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{ProcessId}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    /// <returns>The logger, or <see langword="null"/> if the log folder can't be created.</returns>
    public static Serilog.ILogger? TryCreateLogger(FileLoggingOptions options, string logsDirectory, List<StartupWarning> warnings)
    {
        try
        {
            Directory.CreateDirectory(logsDirectory);

            return new LoggerConfiguration()
                .MinimumLevel.Is(options.MinimumLevel)
                .Enrich.WithProperty("ProcessId", Environment.ProcessId)
                .WriteTo.File(
                    Path.Combine(logsDirectory, FileNamePattern),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: Math.Max(1, options.RetainedFileCountLimit),
                    // Several links clicked in quick succession run as several processes writing the same file.
                    shared: true,
                    outputTemplate: OutputTemplate,
                    formatProvider: CultureInfo.InvariantCulture)
                .CreateLogger();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // Nowhere to log this; the app keeps working without a log file.
            warnings.Add(new StartupWarning("File logging is unavailable", ex));
            return null;
        }
    }
}
