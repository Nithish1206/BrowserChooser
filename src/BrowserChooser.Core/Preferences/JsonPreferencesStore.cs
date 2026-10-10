using System.Text.Json;
using BrowserChooser.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace BrowserChooser.Core.Preferences;

/// <summary>Stores preferences in %LOCALAPPDATA%\BrowserChooser\preferences.json.</summary>
internal sealed class JsonPreferencesStore(AppEnvironment environment, ILogger<JsonPreferencesStore> logger) : IPreferencesStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public UserPreferences Load()
    {
        string path = environment.PreferencesPath;
        if (!File.Exists(path))
            return UserPreferences.Empty;

        try
        {
            return JsonSerializer.Deserialize<UserPreferences>(File.ReadAllText(path), SerializerOptions) ?? UserPreferences.Empty;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            logger.LogWarning(ex, "Could not read preferences from {Path}; using defaults", path);
            return UserPreferences.Empty;
        }
    }

    public void Save(UserPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        string path = environment.PreferencesPath;
        // Write a temporary file and swap it in, so a crash or a second instance never leaves a half-written file.
        string temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            Directory.CreateDirectory(environment.DataDirectory);
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(preferences, SerializerOptions));
            File.Move(temporaryPath, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not save preferences to {Path}", path);
            TryDelete(temporaryPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Leftover temporary file; harmless.
        }
    }
}
