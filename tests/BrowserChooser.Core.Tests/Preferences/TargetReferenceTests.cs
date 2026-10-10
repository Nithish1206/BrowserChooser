using BrowserChooser.Core.Models;
using BrowserChooser.Core.Preferences;
using BrowserChooser.Core.Tests.TestSupport;

namespace BrowserChooser.Core.Tests.Preferences;

public class TargetReferenceTests
{
    private static readonly BrowserTarget EdgeWork = new(TestBrowsers.Edge, new BrowserProfile("Profile 1", "Work"));
    private static readonly BrowserTarget EdgeHome = new(TestBrowsers.Edge, new BrowserProfile("Profile 2", "Home"));
    private static readonly BrowserTarget Chrome = new(TestBrowsers.Chrome, Profile: null);

    [Fact]
    public void Finds_the_same_browser_and_profile() =>
        Assert.Equal(EdgeHome, TargetReference.From(EdgeHome).FindIn([EdgeWork, EdgeHome, Chrome]));

    [Fact]
    public void Matches_by_id_even_after_the_browser_moved()
    {
        BrowserTarget moved = EdgeWork with { Browser = TestBrowsers.Edge with { ExecutablePath = @"D:\Edge\msedge.exe" } };

        Assert.Equal(moved, TargetReference.From(EdgeWork).FindIn([moved]));
    }

    [Fact]
    public void Returns_null_when_the_profile_is_gone() =>
        Assert.Null(TargetReference.From(EdgeHome).FindIn([EdgeWork, Chrome]));

    [Fact]
    public void A_browser_without_profiles_matches_only_the_profile_less_target() =>
        Assert.Equal(Chrome, TargetReference.From(Chrome).FindIn([EdgeWork, Chrome]));
}
