namespace BrowserChooser.Core;

/// <summary>Names under which Browser Chooser identifies itself to Windows and on disk.</summary>
public static class AppIdentity
{
    public const string DisplayName = "Browser Chooser";

    public const string Description = "Asks which browser and profile should open each link you click.";

    /// <summary>Key name under StartMenuInternet and RegisteredApplications.</summary>
    public const string RegistrationKey = "BrowserChooser";

    /// <summary>ProgID that http and https links are associated with.</summary>
    public const string ProgId = "BrowserChooserURL";

    /// <summary>Folder under %LOCALAPPDATA% that holds logs and per-user settings.</summary>
    public const string DataFolderName = "BrowserChooser";
}
