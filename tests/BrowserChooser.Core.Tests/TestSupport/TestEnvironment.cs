namespace BrowserChooser.Core.Tests.TestSupport;

internal static class TestEnvironment
{
    public const string LocalAppData = @"C:\Users\Test\AppData\Local";

    public static AppEnvironment Create(string executablePath = @"C:\Apps\BrowserChooser\BrowserChooser.exe") => new(
        executablePath,
        LocalAppData,
        RoamingAppData: @"C:\Users\Test\AppData\Roaming",
        ProgramFiles: @"C:\Program Files",
        ProgramFilesX86: @"C:\Program Files (x86)");
}
