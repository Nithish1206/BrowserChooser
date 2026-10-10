using BrowserChooser.Core.Models;

namespace BrowserChooser.Core.Abstractions;

/// <summary>A place browsers can be discovered from, such as the Windows registry or the settings file.</summary>
public interface IBrowserSource
{
    /// <summary>Returns the browsers this source knows about. Unreadable entries are skipped, not thrown.</summary>
    IEnumerable<InstalledBrowser> GetBrowsers();
}
