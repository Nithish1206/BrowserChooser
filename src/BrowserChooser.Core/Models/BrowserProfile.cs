namespace BrowserChooser.Core.Models;

/// <summary>A user profile inside a browser.</summary>
/// <param name="Id">
/// The value the browser expects on its command line: the Chromium profile directory ("Default", "Profile 1")
/// or the Firefox profile name.
/// </param>
/// <param name="DisplayName">The name the user gave the profile.</param>
public sealed record BrowserProfile(string Id, string DisplayName);
