using System.Text.Json;
using BrowserChooser.Core.Models;

namespace BrowserChooser.Core.Profiles;

/// <summary>
/// Reads the profile list from Chromium's "Local State" JSON: <c>profile.info_cache</c> holds one entry per profile
/// directory, and <c>profile.profiles_order</c> is the order the browser's own profile menu uses.
/// </summary>
internal static class ChromiumLocalStateParser
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    /// <exception cref="JsonException">The content is not valid JSON.</exception>
    public static IReadOnlyList<BrowserProfile> Parse(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json, DocumentOptions);

        if (!TryGetObject(document.RootElement, "profile", out JsonElement profile) ||
            !TryGetObject(profile, "info_cache", out JsonElement infoCache))
        {
            return [];
        }

        var profiles = new List<BrowserProfile>();
        foreach (JsonProperty entry in infoCache.EnumerateObject())
        {
            if (entry.Value.ValueKind != JsonValueKind.Object)
                continue;

            // Guest and system profiles: the browser hides them from its own profile menu, so do we.
            if (IsTrue(entry.Value, "is_ephemeral") || IsTrue(entry.Value, "is_omitted_from_profile_list"))
                continue;

            string displayName = GetString(entry.Value, "name") ?? entry.Name;
            profiles.Add(new BrowserProfile(entry.Name, displayName));
        }

        List<string> order = GetProfilesOrder(profile);
        return profiles
            .OrderBy(p => order.FindIndex(id => id.Equals(p.Id, StringComparison.OrdinalIgnoreCase)) is var index and >= 0 ? index : int.MaxValue)
            .ThenBy(p => p.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static List<string> GetProfilesOrder(JsonElement profile)
    {
        if (!profile.TryGetProperty("profiles_order", out JsonElement order) || order.ValueKind != JsonValueKind.Array)
            return [];

        return order.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToList();
    }

    private static bool TryGetObject(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(propertyName, out value) &&
               value.ValueKind == JsonValueKind.Object;
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement value) &&
        value.ValueKind == JsonValueKind.String &&
        !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!.Trim()
            : null;

    private static bool IsTrue(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.True;
}
