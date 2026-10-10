namespace BrowserChooser.Core.Exceptions;

/// <summary>A browser's profile data exists but could not be read or parsed.</summary>
public sealed class ProfileReadException(string message, string sourcePath, Exception? innerException)
    : BrowserChooserException(message, innerException)
{
    /// <summary>The file that could not be read.</summary>
    public string SourcePath { get; } = sourcePath;
}
