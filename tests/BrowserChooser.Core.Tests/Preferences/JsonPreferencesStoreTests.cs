using BrowserChooser.Core.Preferences;
using Microsoft.Extensions.Logging.Abstractions;

namespace BrowserChooser.Core.Tests.Preferences;

public sealed class JsonPreferencesStoreTests : IDisposable
{
    private readonly string _localAppData = Path.Combine(Path.GetTempPath(), $"browserchooser-test-{Guid.NewGuid():N}");
    private readonly AppEnvironment _environment;
    private readonly JsonPreferencesStore _store;

    public JsonPreferencesStoreTests()
    {
        _environment = new AppEnvironment(@"C:\Apps\BrowserChooser.exe", _localAppData, _localAppData, @"C:\Program Files", @"C:\Program Files (x86)");
        _store = new JsonPreferencesStore(_environment, NullLogger<JsonPreferencesStore>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_localAppData))
            Directory.Delete(_localAppData, recursive: true);
    }

    [Fact]
    public void Nothing_saved_yet_means_empty_preferences() =>
        Assert.Equal(UserPreferences.Empty, _store.Load());

    [Fact]
    public void Saved_preferences_load_back()
    {
        var preferences = new UserPreferences(LastUsed: new TargetReference("Microsoft Edge", "Profile 1", "Microsoft Edge — Work"));

        _store.Save(preferences);

        Assert.Equal(preferences, _store.Load());
    }

    [Fact]
    public void A_file_from_an_older_version_still_loads()
    {
        // Earlier versions also stored an "always" choice; unknown keys are ignored.
        Directory.CreateDirectory(_environment.DataDirectory);
        File.WriteAllText(_environment.PreferencesPath, """
            {
              "lastUsed": { "browserId": "Google Chrome", "profileId": "Default", "displayName": "Google Chrome — Me" },
              "always": { "browserId": "Microsoft Edge", "profileId": "Profile 1", "displayName": "Microsoft Edge — Work" }
            }
            """);

        Assert.Equal(new TargetReference("Google Chrome", "Default", "Google Chrome — Me"), _store.Load().LastUsed);
    }

    [Fact]
    public void Saving_replaces_the_previous_file_and_leaves_no_temporary_files()
    {
        _store.Save(new UserPreferences(LastUsed: new TargetReference("A", null, "A")));
        _store.Save(new UserPreferences(LastUsed: new TargetReference("B", null, "B")));

        Assert.Equal("B", _store.Load().LastUsed?.BrowserId);
        Assert.Equal([_environment.PreferencesPath], Directory.GetFiles(_environment.DataDirectory));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "lastUsed": 42 }""")]
    [InlineData("")]
    public void A_corrupt_file_falls_back_to_empty_preferences(string content)
    {
        Directory.CreateDirectory(_environment.DataDirectory);
        File.WriteAllText(_environment.PreferencesPath, content);

        Assert.Equal(UserPreferences.Empty, _store.Load());
    }
}
