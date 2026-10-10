using BrowserChooser.Core.Exceptions;
using BrowserChooser.Core.Models;

namespace BrowserChooser.Core.Abstractions;

/// <summary>Reads the profiles of browsers that belong to one <see cref="BrowserFamily"/>.</summary>
public interface IProfileProvider
{
    BrowserFamily Family { get; }

    /// <returns>The browser's profiles, or an empty list if it has no profile data yet.</returns>
    /// <exception cref="ProfileReadException">Profile data exists but could not be read or parsed.</exception>
    IReadOnlyList<BrowserProfile> GetProfiles(InstalledBrowser browser);
}
