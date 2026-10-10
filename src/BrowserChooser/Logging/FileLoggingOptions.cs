using Serilog.Events;

namespace BrowserChooser.Logging;

/// <summary>The "Logging" section of appsettings.json.</summary>
internal sealed class FileLoggingOptions
{
    public const string SectionName = "Logging";

    /// <summary>Verbose, Debug, Information, Warning, Error or Fatal.</summary>
    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;

    /// <summary>One file is written per day; older files beyond this count are deleted.</summary>
    public int RetainedFileCountLimit { get; set; } = 14;
}
