using BrowserChooser.Core.Models;

namespace BrowserChooser.Core.Discovery;

/// <summary>Works out a browser's family and, for Chromium browsers, where its profiles live.</summary>
internal sealed class BrowserClassifier(AppEnvironment environment)
{
    private const string FirefoxExecutableName = "firefox.exe";

    public InstalledBrowser Classify(string id, string name, string executablePath)
    {
        if (Path.GetFileName(executablePath).Equals(FirefoxExecutableName, StringComparison.OrdinalIgnoreCase))
            return new InstalledBrowser(id, name, executablePath, BrowserFamily.Firefox);

        // A "Local State" file is what identifies a Chromium browser, whatever its executable is called.
        string? userDataPath = ChromiumUserDataLocator.Locate(executablePath, environment, File.Exists);
        return userDataPath is null
            ? new InstalledBrowser(id, name, executablePath, BrowserFamily.Other)
            : new InstalledBrowser(id, name, executablePath, BrowserFamily.Chromium, userDataPath);
    }
}
