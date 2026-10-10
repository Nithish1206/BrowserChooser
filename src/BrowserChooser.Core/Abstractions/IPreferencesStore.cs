using BrowserChooser.Core.Preferences;

namespace BrowserChooser.Core.Abstractions;

/// <summary>
/// Loads and saves <see cref="UserPreferences"/>. Preferences are a convenience: failures are logged and never
/// thrown, so they can't stop a link from opening.
/// </summary>
public interface IPreferencesStore
{
    /// <returns>The saved preferences, or <see cref="UserPreferences.Empty"/> if there are none or they can't be read.</returns>
    UserPreferences Load();

    void Save(UserPreferences preferences);
}
