using BrowserChooser.Core.Discovery;
using BrowserChooser.Core.Tests.TestSupport;

namespace BrowserChooser.Core.Tests.Discovery;

public class ChromiumUserDataLocatorTests
{
    private static readonly AppEnvironment Environment = TestEnvironment.Create();

    [Theory]
    [InlineData(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe", @"Microsoft\Edge\User Data")]
    [InlineData(@"C:\Program Files\Google\Chrome\Application\chrome.exe", @"Google\Chrome\User Data")]
    [InlineData(@"C:\Users\Test\AppData\Local\Google\Chrome SxS\Application\chrome.exe", @"Google\Chrome SxS\User Data")]
    [InlineData(@"C:\Users\Test\AppData\Local\BraveSoftware\Brave-Browser\Application\brave.exe", @"BraveSoftware\Brave-Browser\User Data")]
    [InlineData(@"C:\Users\Test\AppData\Local\Vivaldi\Application\vivaldi.exe", @"Vivaldi\User Data")]
    public void Finds_the_user_data_folder_in_local_app_data(string executablePath, string expectedUnderLocalAppData)
    {
        string expected = Path.Combine(TestEnvironment.LocalAppData, expectedUnderLocalAppData);

        string? userData = ChromiumUserDataLocator.Locate(executablePath, Environment, LocalStateExistsIn(expected));

        Assert.Equal(expected, userData);
    }

    [Fact]
    public void Returns_null_when_there_is_no_local_state() =>
        Assert.Null(ChromiumUserDataLocator.Locate(@"C:\Program Files\Google\Chrome\Application\chrome.exe", Environment, _ => false));

    [Fact]
    public void Returns_null_for_executables_outside_the_install_roots() =>
        Assert.Null(ChromiumUserDataLocator.Locate(@"D:\Portable\Chromium\chrome.exe", Environment, _ => true));

    [Fact]
    public void Program_Files_does_not_match_Program_Files_x86()
    {
        // With a naive prefix check, "C:\Program Files" would match and produce " (x86)\Vendor\Product".
        string expected = Path.Combine(TestEnvironment.LocalAppData, @"Vendor\Product\User Data");

        string? userData = ChromiumUserDataLocator.Locate(
            @"C:\Program Files (x86)\Vendor\Product\Application\product.exe", Environment, LocalStateExistsIn(expected));

        Assert.Equal(expected, userData);
    }

    private static Func<string, bool> LocalStateExistsIn(string userDataPath) =>
        path => path.Equals(Path.Combine(userDataPath, "Local State"), StringComparison.OrdinalIgnoreCase);
}
