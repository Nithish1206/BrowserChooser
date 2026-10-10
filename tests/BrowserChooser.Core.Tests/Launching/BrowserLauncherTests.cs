using System.ComponentModel;
using BrowserChooser.Core.Exceptions;
using BrowserChooser.Core.Launching;
using BrowserChooser.Core.Models;
using BrowserChooser.Core.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace BrowserChooser.Core.Tests.Launching;

public sealed class BrowserLauncherTests : IDisposable
{
    private static readonly Uri Link = new("https://example.com/");

    private readonly string _executablePath = Path.Combine(Path.GetTempPath(), $"browserchooser-test-{Guid.NewGuid():N}.exe");
    private readonly RecordingProcessStarter _processStarter = new();
    private readonly BrowserLauncher _launcher;

    public BrowserLauncherTests()
    {
        File.WriteAllBytes(_executablePath, []);
        _launcher = new BrowserLauncher(_processStarter, NullLogger<BrowserLauncher>.Instance);
    }

    public void Dispose() => File.Delete(_executablePath);

    [Fact]
    public void Starts_the_browser_executable_directly_with_profile_arguments()
    {
        var target = new BrowserTarget(TestBrowsers.Chrome with { ExecutablePath = _executablePath }, new BrowserProfile("Profile 1", "Work"));

        _launcher.Launch(target, Link);

        var startInfo = Assert.Single(_processStarter.Started);
        Assert.Equal(_executablePath, startInfo.FileName);
        Assert.False(startInfo.UseShellExecute);
        Assert.Equal(["--profile-directory=Profile 1", "https://example.com/"], startInfo.ArgumentList);
    }

    [Fact]
    public void Missing_executable_fails_without_starting_anything()
    {
        var target = new BrowserTarget(TestBrowsers.Chrome with { ExecutablePath = _executablePath + ".missing" }, Profile: null);

        var exception = Assert.Throws<BrowserLaunchException>(() => _launcher.Launch(target, Link));

        Assert.Same(target, exception.Target);
        Assert.Empty(_processStarter.Started);
    }

    [Fact]
    public void Start_failures_are_wrapped_with_the_target()
    {
        _processStarter.ExceptionToThrow = new Win32Exception(5, "Access is denied");
        var target = new BrowserTarget(TestBrowsers.Chrome with { ExecutablePath = _executablePath }, Profile: null);

        var exception = Assert.Throws<BrowserLaunchException>(() => _launcher.Launch(target, Link));

        Assert.Same(target, exception.Target);
        Assert.IsType<Win32Exception>(exception.InnerException);
    }
}
