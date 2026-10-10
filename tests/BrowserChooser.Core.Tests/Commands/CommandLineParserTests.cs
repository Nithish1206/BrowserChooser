using BrowserChooser.Core.Commands;

namespace BrowserChooser.Core.Tests.Commands;

public class CommandLineParserTests
{
    [Fact]
    public void No_arguments_shows_the_home_dialog() =>
        Assert.IsType<StartupCommand.ShowHome>(CommandLineParser.Parse([]));

    [Theory]
    [InlineData("--register")]
    [InlineData("/REGISTER")]
    public void Register_switch_opens_settings_afterwards(string argument) =>
        Assert.True(Assert.IsType<StartupCommand.Register>(CommandLineParser.Parse([argument])).OpenSettings);

    [Theory]
    [InlineData("--quiet")]
    [InlineData("/Quiet")]
    public void Quiet_registration_does_not_open_settings(string quiet) =>
        Assert.False(Assert.IsType<StartupCommand.Register>(CommandLineParser.Parse(["--register", quiet])).OpenSettings);

    [Theory]
    [InlineData("--unregister")]
    [InlineData("/Unregister")]
    public void Unregister_switch(string argument) =>
        Assert.IsType<StartupCommand.Unregister>(CommandLineParser.Parse([argument]));

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("/?")]
    [InlineData("")]
    public void Help_shows_the_home_dialog(string argument) =>
        Assert.IsType<StartupCommand.ShowHome>(CommandLineParser.Parse([argument]));

    [Fact]
    public void Link_argument_opens_the_link()
    {
        var command = Assert.IsType<StartupCommand.OpenLink>(CommandLineParser.Parse(["https://example.com/page"]));

        Assert.Equal(new Uri("https://example.com/page"), command.Url);
    }

    [Fact]
    public void Only_the_first_argument_counts() =>
        Assert.IsType<StartupCommand.OpenLink>(CommandLineParser.Parse(["https://example.com", "--register"]));

    [Fact]
    public void Anything_else_is_rejected()
    {
        var command = Assert.IsType<StartupCommand.InvalidLink>(CommandLineParser.Parse([@"C:\file.txt"]));

        Assert.Equal(@"C:\file.txt", command.Value);
    }
}
