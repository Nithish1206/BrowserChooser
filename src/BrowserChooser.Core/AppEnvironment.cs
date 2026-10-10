namespace BrowserChooser.Core;

/// <summary>
/// The running executable and the well-known folders Browser Chooser reads from. Injected instead of read from
/// <see cref="Environment"/> directly so that path logic can be tested.
/// </summary>
public sealed record AppEnvironment(
    string ExecutablePath,
    string LocalAppData,
    string RoamingAppData,
    string ProgramFiles,
    string ProgramFilesX86)
{
    public string DataDirectory => Path.Combine(LocalAppData, AppIdentity.DataFolderName);

    public string LogsDirectory => Path.Combine(DataDirectory, "Logs");

    /// <summary>Optional per-user settings that override the appsettings.json shipped next to the executable.</summary>
    public string UserSettingsPath => Path.Combine(DataDirectory, "appsettings.json");

    /// <summary>Remembered choices, such as the last-used browser.</summary>
    public string PreferencesPath => Path.Combine(DataDirectory, "preferences.json");

    public static AppEnvironment FromCurrentProcess() => new(
        Environment.ProcessPath ?? throw new InvalidOperationException("The path of the running executable is unknown."),
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
}
