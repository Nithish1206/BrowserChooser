using System.Security;
using BrowserChooser.Core.Abstractions;
using BrowserChooser.Core.Discovery;
using BrowserChooser.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace BrowserChooser.Core.Registration;

/// <summary>
/// Registers Browser Chooser as a browser for the current user (HKCU, so no admin rights are needed), which makes it
/// appear in Settings › Apps › Default apps. Windows does not let an app make itself the default browser; the user
/// has to pick it there.
/// </summary>
internal sealed class WindowsAppRegistrar : IAppRegistrar
{
    private const string ClientKeyPath = @"Software\Clients\StartMenuInternet\" + AppIdentity.RegistrationKey;
    private const string CapabilitiesKeyPath = ClientKeyPath + @"\Capabilities";
    private const string RegisteredApplicationsKeyPath = @"Software\RegisteredApplications";
    private const string ProgIdKeyPath = @"Software\Classes\" + AppIdentity.ProgId;
    private const string ProgIdCommandKeyPath = ProgIdKeyPath + @"\shell\open\command";

    private readonly RegistryKey _root;
    private readonly bool _notifyShell;
    private readonly ILogger<WindowsAppRegistrar> _logger;

    public WindowsAppRegistrar(ILogger<WindowsAppRegistrar> logger)
        : this(Registry.CurrentUser, logger, notifyShell: true)
    {
    }

    /// <param name="root">The hive to write to. Tests pass a scratch key instead of HKEY_CURRENT_USER.</param>
    internal WindowsAppRegistrar(RegistryKey root, ILogger<WindowsAppRegistrar> logger, bool notifyShell)
    {
        _root = root;
        _logger = logger;
        _notifyShell = notifyShell;
    }

    public RegistrationStatus GetStatus(string executablePath)
    {
        try
        {
            using RegistryKey? registeredApplications = _root.OpenSubKey(RegisteredApplicationsKeyPath);
            using RegistryKey? command = _root.OpenSubKey(ProgIdCommandKeyPath);

            if (registeredApplications?.GetValue(AppIdentity.RegistrationKey) is null ||
                command?.GetValue(null) is not string registeredCommand)
            {
                return RegistrationStatus.NotRegistered;
            }

            string? registeredExecutable = ShellCommandParser.GetExecutablePath(registeredCommand);
            return string.Equals(registeredExecutable, executablePath, StringComparison.OrdinalIgnoreCase)
                ? RegistrationStatus.Registered
                : RegistrationStatus.RegisteredElsewhere;
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            _logger.LogWarning(ex, "Could not read the registration status");
            return RegistrationStatus.NotRegistered;
        }
    }

    public void Register(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        string icon = $"{executablePath},0";
        try
        {
            using (RegistryKey client = _root.CreateSubKey(ClientKeyPath))
            {
                client.SetValue(null, AppIdentity.DisplayName);
                SetDefault(client, "DefaultIcon", icon);
                // Started from the Start menu "browser" entry: no link, so the app shows its home dialog.
                SetDefault(client, @"shell\open\command", $"\"{executablePath}\"");

                using RegistryKey capabilities = client.CreateSubKey("Capabilities");
                capabilities.SetValue("ApplicationName", AppIdentity.DisplayName);
                capabilities.SetValue("ApplicationDescription", AppIdentity.Description);
                capabilities.SetValue("ApplicationIcon", icon);

                using (RegistryKey startMenu = capabilities.CreateSubKey("StartMenu"))
                    startMenu.SetValue("StartMenuInternet", AppIdentity.RegistrationKey);

                using RegistryKey urlAssociations = capabilities.CreateSubKey("URLAssociations");
                urlAssociations.SetValue("http", AppIdentity.ProgId);
                urlAssociations.SetValue("https", AppIdentity.ProgId);
            }

            using (RegistryKey progId = _root.CreateSubKey(ProgIdKeyPath))
            {
                progId.SetValue(null, $"{AppIdentity.DisplayName} URL");
                SetDefault(progId, "DefaultIcon", icon);
                SetDefault(progId, @"shell\open\command", $"\"{executablePath}\" \"%1\"");
            }

            using (RegistryKey registeredApplications = _root.CreateSubKey(RegisteredApplicationsKeyPath))
                registeredApplications.SetValue(AppIdentity.RegistrationKey, CapabilitiesKeyPath);
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            throw new AppRegistrationException("Could not write the browser registration to the registry.", ex);
        }

        NotifyShell();
        _logger.LogInformation("Registered {App} as a browser for {ExecutablePath}", AppIdentity.DisplayName, executablePath);
    }

    public void Unregister()
    {
        try
        {
            _root.DeleteSubKeyTree(ClientKeyPath, throwOnMissingSubKey: false);
            _root.DeleteSubKeyTree(ProgIdKeyPath, throwOnMissingSubKey: false);

            using RegistryKey? registeredApplications = _root.OpenSubKey(RegisteredApplicationsKeyPath, writable: true);
            registeredApplications?.DeleteValue(AppIdentity.RegistrationKey, throwOnMissingValue: false);
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            throw new AppRegistrationException("Could not remove the browser registration from the registry.", ex);
        }

        NotifyShell();
        _logger.LogInformation("Removed the {App} browser registration", AppIdentity.DisplayName);
    }

    private static void SetDefault(RegistryKey parent, string subKeyPath, string value)
    {
        using RegistryKey key = parent.CreateSubKey(subKeyPath);
        key.SetValue(null, value);
    }

    /// <summary>Tells Explorer and Settings to reload associations so the change shows up immediately.</summary>
    private void NotifyShell()
    {
        if (_notifyShell)
            NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
    }
}
