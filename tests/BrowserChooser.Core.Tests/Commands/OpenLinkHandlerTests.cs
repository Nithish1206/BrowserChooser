using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Commands;
using BrowserChooser.Core.Exceptions;
using BrowserChooser.Core.Models;
using BrowserChooser.Core.Preferences;
using BrowserChooser.Core.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace BrowserChooser.Core.Tests.Commands;

public class OpenLinkHandlerTests
{
    private static readonly Uri Link = new("https://example.com/");
    private static readonly BrowserTarget EdgeWork = new(TestBrowsers.Edge, new BrowserProfile("Profile 1", "Work"));
    private static readonly BrowserTarget Chrome = new(TestBrowsers.Chrome, Profile: null);

    private readonly FakeBrowserLauncher _launcher = new();
    private readonly FakeUserNotifier _notifier = new();

    [Fact]
    public void Opens_the_link_in_the_chosen_target()
    {
        ExitCode result = CreateHandler([EdgeWork, Chrome], new FakeBrowserPicker(Chrome)).Handle(Link);

        Assert.Equal(ExitCode.Success, result);
        Assert.Equal([Chrome], _launcher.Launched);
    }

    [Fact]
    public void Cancelling_opens_nothing_and_saves_nothing()
    {
        var preferences = new InMemoryPreferencesStore();

        ExitCode result = CreateHandler([EdgeWork], new FakeBrowserPicker((BrowserTarget?)null), preferences).Handle(Link);

        Assert.Equal(ExitCode.Cancelled, result);
        Assert.Empty(_launcher.Launched);
        Assert.Equal(0, preferences.SaveCount);
    }

    [Fact]
    public void Reports_when_no_browsers_are_installed()
    {
        var picker = new FakeBrowserPicker();

        ExitCode result = CreateHandler([], picker).Handle(Link);

        Assert.Equal(ExitCode.NoBrowsersFound, result);
        Assert.Single(_notifier.Errors);
        Assert.Equal(0, picker.TimesShown);
    }

    [Fact]
    public void After_a_failed_launch_the_user_can_choose_another_browser()
    {
        _launcher.Failure = target => target == EdgeWork ? new BrowserLaunchException(target, "Edge is gone") : null;
        _notifier.ChooseAgainAnswer = true;
        var picker = new FakeBrowserPicker(EdgeWork, Chrome);

        ExitCode result = CreateHandler([EdgeWork, Chrome], picker).Handle(Link);

        Assert.Equal(ExitCode.Success, result);
        Assert.Equal([Chrome], _launcher.Launched);
        Assert.Equal(2, picker.TimesShown);
        Assert.Single(_notifier.ChooseAgainPrompts);
    }

    [Fact]
    public void A_failed_launch_ends_the_run_when_the_user_declines_to_choose_again()
    {
        _launcher.Failure = target => new BrowserLaunchException(target, "broken");
        _notifier.ChooseAgainAnswer = false;

        ExitCode result = CreateHandler([EdgeWork], new FakeBrowserPicker(EdgeWork)).Handle(Link);

        Assert.Equal(ExitCode.LaunchFailed, result);
    }

    [Fact]
    public void A_successful_choice_is_remembered_as_last_used()
    {
        var preferences = new InMemoryPreferencesStore();

        CreateHandler([EdgeWork, Chrome], new FakeBrowserPicker(Chrome), preferences).Handle(Link);

        Assert.Equal(TargetReference.From(Chrome), preferences.Current.LastUsed);
    }

    [Fact]
    public void A_failed_choice_is_not_remembered()
    {
        var preferences = new InMemoryPreferencesStore();
        _launcher.Failure = target => new BrowserLaunchException(target, "broken");

        CreateHandler([EdgeWork], new FakeBrowserPicker(EdgeWork), preferences).Handle(Link);

        Assert.Equal(0, preferences.SaveCount);
    }

    [Fact]
    public void The_last_used_choice_is_offered_first()
    {
        var preferences = new InMemoryPreferencesStore(new UserPreferences(LastUsed: TargetReference.From(Chrome)));
        var picker = new FakeBrowserPicker(EdgeWork);

        CreateHandler([EdgeWork, Chrome], picker, preferences).Handle(Link);

        Assert.Equal(Chrome, picker.LastUsedShown);
    }

    [Fact]
    public void A_last_used_browser_that_was_uninstalled_is_ignored()
    {
        var uninstalled = new TargetReference("Vivaldi", ProfileId: null, "Vivaldi");
        var preferences = new InMemoryPreferencesStore(new UserPreferences(LastUsed: uninstalled));
        var picker = new FakeBrowserPicker(Chrome);

        ExitCode result = CreateHandler([EdgeWork, Chrome], picker, preferences).Handle(Link);

        Assert.Equal(ExitCode.Success, result);
        Assert.Null(picker.LastUsedShown);
    }

    private OpenLinkHandler CreateHandler(
        IReadOnlyList<BrowserTarget> targets,
        IBrowserPicker picker,
        IPreferencesStore? preferences = null) =>
        new(new FixedCatalog(targets), picker, _launcher, preferences ?? new InMemoryPreferencesStore(), _notifier,
            NullLogger<OpenLinkHandler>.Instance);

    private sealed class FixedCatalog(IReadOnlyList<BrowserTarget> targets) : IBrowserCatalog
    {
        public IReadOnlyList<BrowserTarget> GetTargets() => targets;
    }
}
