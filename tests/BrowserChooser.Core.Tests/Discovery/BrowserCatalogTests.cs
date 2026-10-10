using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Configuration;
using BrowserChooser.Core.Discovery;
using BrowserChooser.Core.Exceptions;
using BrowserChooser.Core.Models;
using BrowserChooser.Core.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BrowserChooser.Core.Tests.Discovery;

public class BrowserCatalogTests
{
    [Fact]
    public void Each_browser_becomes_one_target_per_profile()
    {
        BrowserCatalog catalog = CreateCatalog(
            [new FakeBrowserSource(TestBrowsers.Edge)],
            [new FakeProfileProvider(BrowserFamily.Chromium, _ => [new("Profile 1", "Work"), new("Profile 2", "Home")])]);

        IReadOnlyList<BrowserTarget> targets = catalog.GetTargets();

        Assert.Equal(["Microsoft Edge — Work", "Microsoft Edge — Home"], targets.Select(t => t.DisplayName));
    }

    [Fact]
    public void Browser_without_profiles_is_offered_once()
    {
        BrowserCatalog catalog = CreateCatalog(
            [new FakeBrowserSource(TestBrowsers.Chrome)],
            [new FakeProfileProvider(BrowserFamily.Chromium, _ => [])]);

        BrowserTarget target = Assert.Single(catalog.GetTargets());

        Assert.Null(target.Profile);
    }

    [Fact]
    public void Browser_with_unreadable_profiles_is_still_offered()
    {
        BrowserCatalog catalog = CreateCatalog(
            [new FakeBrowserSource(TestBrowsers.Chrome)],
            [new FakeProfileProvider(BrowserFamily.Chromium, _ => throw new ProfileReadException("corrupt", "Local State", null))]);

        BrowserTarget target = Assert.Single(catalog.GetTargets());

        Assert.Equal(TestBrowsers.Chrome, target.Browser);
        Assert.Null(target.Profile);
    }

    [Fact]
    public void Duplicates_exclusions_and_Browser_Chooser_itself_are_removed()
    {
        var registeredSelf = new InstalledBrowser(AppIdentity.RegistrationKey, "Browser Chooser", @"C:\Apps\BrowserChooser\BrowserChooser.exe", BrowserFamily.Other);
        var otherCopyOfSelf = new InstalledBrowser("Copy", "Old copy", @"D:\Old\BrowserChooser.exe", BrowserFamily.Other);
        var edgeFromOtherRegistryView = TestBrowsers.Edge with { Id = "Microsoft Edge (32-bit view)" };

        BrowserCatalog catalog = CreateCatalog(
            [new FakeBrowserSource(TestBrowsers.Edge, edgeFromOtherRegistryView, TestBrowsers.InternetExplorer, registeredSelf, otherCopyOfSelf)],
            [],
            excludedBrowsers: ["iexplore.exe"]);

        Assert.Equal([TestBrowsers.Edge], catalog.GetBrowsers());
    }

    [Theory]
    [InlineData("Internet Explorer")]
    [InlineData("IEXPLORE.EXE")]
    [InlineData("iexplore.exe")]
    public void Exclusions_match_name_registry_key_or_executable(string exclusion)
    {
        BrowserCatalog catalog = CreateCatalog([new FakeBrowserSource(TestBrowsers.InternetExplorer)], [], excludedBrowsers: [exclusion]);

        Assert.Empty(catalog.GetBrowsers());
    }

    [Fact]
    public void Browsers_are_sorted_by_name()
    {
        BrowserCatalog catalog = CreateCatalog([new FakeBrowserSource(TestBrowsers.Firefox, TestBrowsers.Edge, TestBrowsers.Chrome)], []);

        Assert.Equal(["Google Chrome", "Microsoft Edge", "Mozilla Firefox"], catalog.GetBrowsers().Select(b => b.Name));
    }

    private static BrowserCatalog CreateCatalog(
        IEnumerable<IBrowserSource> sources,
        IEnumerable<IProfileProvider> profileProviders,
        List<string>? excludedBrowsers = null) =>
        new(
            sources,
            profileProviders,
            Options.Create(new BrowserChooserOptions { ExcludedBrowsers = excludedBrowsers ?? [] }),
            TestEnvironment.Create(),
            NullLogger<BrowserCatalog>.Instance);
}
