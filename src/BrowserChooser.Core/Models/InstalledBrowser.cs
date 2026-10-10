namespace BrowserChooser.Core.Models;

/// <summary>A browser installed on this PC.</summary>
/// <param name="Id">Stable identifier: the registry key name, or "custom:{Name}" for browsers from settings.</param>
/// <param name="Name">Display name, for example "Microsoft Edge".</param>
/// <param name="ExecutablePath">Full path to the browser executable.</param>
/// <param name="Family">Engine family; decides how profiles are read and passed to the browser.</param>
/// <param name="UserDataPath">Chromium "User Data" folder, or <see langword="null"/> when unknown or not applicable.</param>
public sealed record InstalledBrowser(
    string Id,
    string Name,
    string ExecutablePath,
    BrowserFamily Family,
    string? UserDataPath = null);
