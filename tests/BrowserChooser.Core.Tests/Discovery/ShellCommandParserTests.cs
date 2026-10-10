using BrowserChooser.Core.Discovery;

namespace BrowserChooser.Core.Tests.Discovery;

public class ShellCommandParserTests
{
    [Theory]
    [InlineData("\"C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe\" --single-argument %1", @"C:\Program Files\Google\Chrome\Application\chrome.exe")]
    [InlineData("\"C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe\"", @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe")]
    [InlineData(@"C:\Program Files\Internet Explorer\iexplore.exe", @"C:\Program Files\Internet Explorer\iexplore.exe")]
    [InlineData("C:\\Program Files\\Mozilla Firefox\\firefox.exe -osint -url \"%1\"", @"C:\Program Files\Mozilla Firefox\firefox.exe")]
    [InlineData(@"C:\Tools\browser", @"C:\Tools\browser")]
    public void Extracts_the_executable(string command, string expected) =>
        Assert.Equal(expected, ShellCommandParser.GetExecutablePath(command));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\"")]
    [InlineData("\"\"")]
    public void Returns_null_for_empty_or_malformed_commands(string? command) =>
        Assert.Null(ShellCommandParser.GetExecutablePath(command));

    [Fact]
    public void Expands_environment_variables() =>
        Assert.Equal(
            Environment.ExpandEnvironmentVariables(@"%SystemRoot%\explorer.exe"),
            ShellCommandParser.GetExecutablePath(@"""%SystemRoot%\explorer.exe"" /n"));
}
