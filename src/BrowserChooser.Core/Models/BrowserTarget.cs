namespace BrowserChooser.Core.Models;

/// <summary>One choice in the picker: a browser, optionally pinned to one of its profiles.</summary>
/// <param name="Profile">
/// The profile to open the link in, or <see langword="null"/> to let the browser use its last-used profile.
/// </param>
public sealed record BrowserTarget(InstalledBrowser Browser, BrowserProfile? Profile)
{
    public string DisplayName => Profile is null ? Browser.Name : $"{Browser.Name} — {Profile.DisplayName}";
}
