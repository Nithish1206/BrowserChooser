using System.Diagnostics;
using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Models;
using BrowserChooser.Core.Preferences;

namespace BrowserChooser.Core.Tests.TestSupport;

internal static class TestBrowsers
{
    public static InstalledBrowser Edge { get; } = new(
        "Microsoft Edge", "Microsoft Edge", @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        BrowserFamily.Chromium, @"C:\Users\Test\AppData\Local\Microsoft\Edge\User Data");

    public static InstalledBrowser Chrome { get; } = new(
        "Google Chrome", "Google Chrome", @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        BrowserFamily.Chromium, @"C:\Users\Test\AppData\Local\Google\Chrome\User Data");

    public static InstalledBrowser Firefox { get; } = new(
        "FIREFOX.EXE", "Mozilla Firefox", @"C:\Program Files\Mozilla Firefox\firefox.exe", BrowserFamily.Firefox);

    public static InstalledBrowser InternetExplorer { get; } = new(
        "IEXPLORE.EXE", "Internet Explorer", @"C:\Program Files\Internet Explorer\iexplore.exe", BrowserFamily.Other);
}

internal sealed class FakeBrowserSource(params InstalledBrowser[] browsers) : IBrowserSource
{
    public IEnumerable<InstalledBrowser> GetBrowsers() => browsers;
}

internal sealed class FakeProfileProvider(BrowserFamily family, Func<InstalledBrowser, IReadOnlyList<BrowserProfile>> getProfiles)
    : IProfileProvider
{
    public BrowserFamily Family => family;

    public IReadOnlyList<BrowserProfile> GetProfiles(InstalledBrowser browser) => getProfiles(browser);
}

/// <summary>Returns the queued choices one per call; <see langword="null"/> means the user cancelled.</summary>
internal sealed class FakeBrowserPicker(params BrowserTarget?[] choices) : IBrowserPicker
{
    private readonly Queue<BrowserTarget?> _choices = new(choices);

    public int TimesShown { get; private set; }

    public BrowserTarget? LastUsedShown { get; private set; }

    public BrowserTarget? Pick(Uri url, IReadOnlyList<BrowserTarget> targets, BrowserTarget? lastUsed)
    {
        TimesShown++;
        LastUsedShown = lastUsed;
        return _choices.Count > 0 ? _choices.Dequeue() : null;
    }
}

internal sealed class InMemoryPreferencesStore(UserPreferences? initial = null) : IPreferencesStore
{
    public UserPreferences Current { get; private set; } = initial ?? UserPreferences.Empty;

    public int SaveCount { get; private set; }

    public UserPreferences Load() => Current;

    public void Save(UserPreferences preferences)
    {
        Current = preferences;
        SaveCount++;
    }
}

internal sealed class FakeBrowserLauncher : IBrowserLauncher
{
    public List<BrowserTarget> Launched { get; } = [];

    /// <summary>Return an exception to throw for a target, or <see langword="null"/> to succeed.</summary>
    public Func<BrowserTarget, Exception?> Failure { get; set; } = _ => null;

    public void Launch(BrowserTarget target, Uri url)
    {
        if (Failure(target) is { } exception)
            throw exception;

        Launched.Add(target);
    }
}

internal sealed class FakeUserNotifier : IUserNotifier
{
    public bool ChooseAgainAnswer { get; set; }

    public List<string> Errors { get; } = [];

    public List<string> ChooseAgainPrompts { get; } = [];

    public List<string> Information { get; } = [];

    public void ShowInformation(string heading, string message) => Information.Add(heading);

    public void ShowError(string heading, string message, Exception? exception = null) => Errors.Add(heading);

    public bool AskToChooseAgain(string heading, string message, Exception? exception = null)
    {
        ChooseAgainPrompts.Add(heading);
        return ChooseAgainAnswer;
    }
}

internal sealed class RecordingProcessStarter : IProcessStarter
{
    public List<ProcessStartInfo> Started { get; } = [];

    public Exception? ExceptionToThrow { get; set; }

    public void Start(ProcessStartInfo startInfo)
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        Started.Add(startInfo);
    }
}
