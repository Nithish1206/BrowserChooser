namespace BrowserChooser.Core.Exceptions;

/// <summary>Registering or unregistering Browser Chooser with Windows failed.</summary>
public sealed class AppRegistrationException(string message, Exception? innerException)
    : BrowserChooserException(message, innerException);
