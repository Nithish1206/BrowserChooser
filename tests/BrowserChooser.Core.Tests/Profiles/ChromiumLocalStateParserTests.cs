using System.Text.Json;
using BrowserChooser.Core.Models;
using BrowserChooser.Core.Profiles;

namespace BrowserChooser.Core.Tests.Profiles;

public class ChromiumLocalStateParserTests
{
    [Fact]
    public void Reads_profiles_in_the_order_of_the_browser_profile_menu()
    {
        const string json = """
            {
              "profile": {
                "profiles_order": [ "Profile 2", "Default" ],
                "info_cache": {
                  "Default":   { "name": "Personal" },
                  "Profile 2": { "name": "Work" },
                  "Profile 9": { "name": "Alpha" }
                }
              }
            }
            """;

        IReadOnlyList<BrowserProfile> profiles = ChromiumLocalStateParser.Parse(json);

        Assert.Equal(
            [new("Profile 2", "Work"), new("Default", "Personal"), new("Profile 9", "Alpha")],
            profiles);
    }

    [Fact]
    public void Skips_guest_and_hidden_profiles()
    {
        const string json = """
            {
              "profile": {
                "info_cache": {
                  "Default":       { "name": "Me" },
                  "Guest Profile": { "name": "Guest", "is_ephemeral": true },
                  "System":        { "name": "System", "is_omitted_from_profile_list": true }
                }
              }
            }
            """;

        BrowserProfile profile = Assert.Single(ChromiumLocalStateParser.Parse(json));

        Assert.Equal("Default", profile.Id);
    }

    [Fact]
    public void Uses_the_directory_name_when_a_profile_has_no_name()
    {
        const string json = """{ "profile": { "info_cache": { "Profile 3": { "name": "  " }, } } }""";

        BrowserProfile profile = Assert.Single(ChromiumLocalStateParser.Parse(json));

        Assert.Equal(new BrowserProfile("Profile 3", "Profile 3"), profile);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("""{ "profile": {} }""")]
    [InlineData("""{ "profile": { "info_cache": [] } }""")]
    [InlineData("""{ "profile": { "info_cache": { "Default": "not an object" } } }""")]
    public void Returns_no_profiles_when_the_profile_section_is_missing_or_unexpected(string json) =>
        Assert.Empty(ChromiumLocalStateParser.Parse(json));

    [Fact]
    public void Throws_on_invalid_json() =>
        Assert.ThrowsAny<JsonException>(() => ChromiumLocalStateParser.Parse("{ \"profile\": "));
}
