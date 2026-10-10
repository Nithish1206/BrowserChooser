using BrowserChooser.Core.Models;
using BrowserChooser.Core.Profiles;

namespace BrowserChooser.Core.Tests.Profiles;

public class FirefoxProfilesIniParserTests
{
    [Fact]
    public void Reads_profile_sections_and_ignores_the_others()
    {
        string ini = string.Join("\r\n",
            "[Install308046B0AF4A39CB]",
            "Default=Profiles/abc.default-release",
            "Locked=1",
            "",
            "[Profile1]",
            "Name=work",
            "IsRelative=1",
            "Path=Profiles/xyz.work",
            "",
            "[Profile0]",
            "Name=default-release",
            "IsRelative=1",
            "Path=Profiles/abc.default-release",
            "",
            "[General]",
            "StartWithLastProfile=1",
            "Version=2");

        IReadOnlyList<BrowserProfile> profiles = FirefoxProfilesIniParser.Parse(ini);

        Assert.Equal([new("work", "work"), new("default-release", "default-release")], profiles);
    }

    [Fact]
    public void Skips_profiles_without_a_name_comments_and_duplicates()
    {
        const string ini = """
            ; comment
            [Profile0]
            Path=Profiles/no-name
            [Profile1]
            # another comment
            Name = Personal
            [Profile2]
            Name=personal
            """;

        BrowserProfile profile = Assert.Single(FirefoxProfilesIniParser.Parse(ini));

        Assert.Equal("Personal", profile.Id);
    }

    [Fact]
    public void Empty_file_has_no_profiles() => Assert.Empty(FirefoxProfilesIniParser.Parse(""));
}
