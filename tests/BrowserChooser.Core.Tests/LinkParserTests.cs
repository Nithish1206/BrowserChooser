namespace BrowserChooser.Core.Tests;

public class LinkParserTests
{
    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/path?q=1#section")]
    [InlineData("  \"https://example.com/quoted\"  ")]
    [InlineData("HTTPS://EXAMPLE.COM")]
    public void Accepts_http_and_https_links(string value)
    {
        Assert.True(LinkParser.TryParse(value, out Uri? url));
        Assert.StartsWith("http", url.Scheme, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("example.com")]
    [InlineData("/relative/path")]
    [InlineData("https://")]
    [InlineData("ftp://example.com/file")]
    [InlineData("file:///C:/Windows/win.ini")]
    [InlineData(@"C:\Windows\notepad.exe")]
    [InlineData(@"\\server\share\file.html")]
    [InlineData("javascript:alert(1)")]
    [InlineData("mailto:someone@example.com")]
    [InlineData("--register")]
    public void Rejects_everything_else(string? value)
    {
        Assert.False(LinkParser.TryParse(value, out Uri? url));
        Assert.Null(url);
    }

    [Fact]
    public void Spaces_and_quotes_are_encoded_so_the_link_stays_a_single_argument()
    {
        Assert.True(LinkParser.TryParse("https://example.com/a b\"c", out Uri? url));

        Assert.Equal("https://example.com/a%20b%22c", url.AbsoluteUri);
    }

    [Fact]
    public void Log_string_leaves_out_path_and_query()
    {
        Assert.True(LinkParser.TryParse("https://accounts.example.com/reset?token=secret#x", out Uri? url));

        Assert.Equal("https://accounts.example.com", LinkParser.ToLogString(url));
    }
}
