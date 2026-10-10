using BrowserChooser.Core.Launching;
using BrowserChooser.Core.Models;
using BrowserChooser.Core.Tests.TestSupport;

namespace BrowserChooser.Core.Tests.Launching;

public class LaunchArgumentsBuilderTests
{
    private static readonly Uri Link = new("https://example.com/a b");

    [Fact]
    public void Chromium_profile_is_passed_as_profile_directory() =>
        Assert.Equal(
            ["--profile-directory=Profile 1", "https://example.com/a%20b"],
            LaunchArgumentsBuilder.Build(new BrowserTarget(TestBrowsers.Edge, new BrowserProfile("Profile 1", "Work")), Link));

    [Fact]
    public void Firefox_profile_is_passed_by_name() =>
        Assert.Equal(
            ["-P", "work", "https://example.com/a%20b"],
            LaunchArgumentsBuilder.Build(new BrowserTarget(TestBrowsers.Firefox, new BrowserProfile("work", "work")), Link));

    [Fact]
    public void Without_a_profile_only_the_link_is_passed() =>
        Assert.Equal(
            ["https://example.com/a%20b"],
            LaunchArgumentsBuilder.Build(new BrowserTarget(TestBrowsers.Chrome, Profile: null), Link));

    [Fact]
    public void Unknown_browsers_get_only_the_link() =>
        Assert.Equal(
            ["https://example.com/a%20b"],
            LaunchArgumentsBuilder.Build(new BrowserTarget(TestBrowsers.InternetExplorer, new BrowserProfile("x", "x")), Link));
}
