using BrowserChooser.Core.Exceptions;
using BrowserChooser.Core.Models;

namespace BrowserChooser.Core.Abstractions;

/// <summary>Opens a link in a specific browser and profile.</summary>
public interface IBrowserLauncher
{
    /// <exception cref="BrowserLaunchException">The browser could not be started.</exception>
    void Launch(BrowserTarget target, Uri url);
}
