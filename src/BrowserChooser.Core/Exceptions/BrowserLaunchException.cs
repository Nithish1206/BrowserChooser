using BrowserChooser.Core.Models;

namespace BrowserChooser.Core.Exceptions;

/// <summary>The chosen browser could not be started.</summary>
public sealed class BrowserLaunchException(BrowserTarget target, string message, Exception? innerException = null)
    : BrowserChooserException(message, innerException)
{
    public BrowserTarget Target { get; } = target;
}
