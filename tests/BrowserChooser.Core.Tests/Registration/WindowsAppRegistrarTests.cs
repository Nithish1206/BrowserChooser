using BrowserChooser.Core.Registration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;

namespace BrowserChooser.Core.Tests.Registration;

/// <summary>Runs the real registrar against a throw-away key under HKCU, never against the live registration.</summary>
public sealed class WindowsAppRegistrarTests : IDisposable
{
    private const string ExecutablePath = @"C:\Apps\BrowserChooser\BrowserChooser.exe";
    private const string ScratchParentPath = @"Software\BrowserChooser.Tests";

    private readonly string _scratchPath = $@"{ScratchParentPath}\{Guid.NewGuid():N}";
    private readonly RegistryKey _root;
    private readonly WindowsAppRegistrar _registrar;

    public WindowsAppRegistrarTests()
    {
        _root = Registry.CurrentUser.CreateSubKey(_scratchPath);
        _registrar = new WindowsAppRegistrar(_root, NullLogger<WindowsAppRegistrar>.Instance, notifyShell: false);
    }

    public void Dispose()
    {
        _root.Dispose();
        Registry.CurrentUser.DeleteSubKeyTree(_scratchPath, throwOnMissingSubKey: false);

        using RegistryKey? parent = Registry.CurrentUser.OpenSubKey(ScratchParentPath);
        if (parent is { SubKeyCount: 0, ValueCount: 0 })
            Registry.CurrentUser.DeleteSubKey(ScratchParentPath, throwOnMissingSubKey: false);
    }

    [Fact]
    public void Register_declares_http_and_https_support_and_the_open_command()
    {
        _registrar.Register(ExecutablePath);

        Assert.Equal(
            @"Software\Clients\StartMenuInternet\BrowserChooser\Capabilities",
            Read(@"Software\RegisteredApplications", "BrowserChooser"));
        Assert.Equal("Browser Chooser", Read(@"Software\Clients\StartMenuInternet\BrowserChooser\Capabilities", "ApplicationName"));
        Assert.Equal("BrowserChooserURL", Read(@"Software\Clients\StartMenuInternet\BrowserChooser\Capabilities\URLAssociations", "http"));
        Assert.Equal("BrowserChooserURL", Read(@"Software\Clients\StartMenuInternet\BrowserChooser\Capabilities\URLAssociations", "https"));
        Assert.Equal($"\"{ExecutablePath}\" \"%1\"", Read(@"Software\Classes\BrowserChooserURL\shell\open\command", null));
    }

    [Fact]
    public void Status_tracks_which_executable_is_registered()
    {
        Assert.Equal(RegistrationStatus.NotRegistered, _registrar.GetStatus(ExecutablePath));

        _registrar.Register(ExecutablePath);

        Assert.Equal(RegistrationStatus.Registered, _registrar.GetStatus(ExecutablePath));
        Assert.Equal(RegistrationStatus.Registered, _registrar.GetStatus(ExecutablePath.ToUpperInvariant()));
        Assert.Equal(RegistrationStatus.RegisteredElsewhere, _registrar.GetStatus(@"D:\Other\BrowserChooser.exe"));
    }

    [Fact]
    public void Unregister_removes_only_Browser_Chooser()
    {
        using (RegistryKey apps = _root.CreateSubKey(@"Software\RegisteredApplications"))
            apps.SetValue("SomeOtherBrowser", @"Software\Clients\StartMenuInternet\Other\Capabilities");
        _registrar.Register(ExecutablePath);

        _registrar.Unregister();

        Assert.Equal(RegistrationStatus.NotRegistered, _registrar.GetStatus(ExecutablePath));
        Assert.Null(_root.OpenSubKey(@"Software\Clients\StartMenuInternet\BrowserChooser"));
        Assert.Null(_root.OpenSubKey(@"Software\Classes\BrowserChooserURL"));
        Assert.Null(Read(@"Software\RegisteredApplications", "BrowserChooser"));
        Assert.NotNull(Read(@"Software\RegisteredApplications", "SomeOtherBrowser"));
    }

    [Fact]
    public void Unregister_when_not_registered_does_nothing()
    {
        _registrar.Unregister();

        Assert.Equal(RegistrationStatus.NotRegistered, _registrar.GetStatus(ExecutablePath));
    }

    private string? Read(string keyPath, string? valueName)
    {
        using RegistryKey? key = _root.OpenSubKey(keyPath);
        return key?.GetValue(valueName) as string;
    }
}
