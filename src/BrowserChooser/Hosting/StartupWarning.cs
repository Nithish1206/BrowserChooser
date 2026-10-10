namespace BrowserChooser.Hosting;

/// <summary>A problem found before logging was ready; it is logged once the logger exists.</summary>
internal sealed record StartupWarning(string Message, Exception Exception);
