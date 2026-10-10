using System.ComponentModel;
using System.Runtime.InteropServices;
using BrowserChooser.Core.Models;
using Microsoft.Extensions.Logging;

namespace BrowserChooser.UI.Chooser;

/// <summary>Icons for picker rows: the profile's own icon where the browser keeps one, otherwise the browser's.</summary>
internal sealed class BrowserIconProvider(ILogger<BrowserIconProvider> logger) : IDisposable
{
    private readonly Dictionary<(string Path, int Size), Bitmap?> _cache = [];

    /// <returns>The icon, or <see langword="null"/> if none could be loaded (the row then draws a placeholder).</returns>
    public Bitmap? GetIcon(BrowserTarget target, int size)
    {
        ArgumentNullException.ThrowIfNull(target);

        string? profileIcon = FindProfileIcon(target);
        return (profileIcon is null ? null : Load(profileIcon, size, isIconFile: true))
            ?? Load(target.Browser.ExecutablePath, size, isIconFile: false);
    }

    public void Dispose()
    {
        foreach (Bitmap? bitmap in _cache.Values)
            bitmap?.Dispose();

        _cache.Clear();
    }

    /// <summary>
    /// Chromium browsers keep the browser icon badged with the profile's avatar in each profile folder ("Edge
    /// Profile.ico", "Google Profile.ico"); their own desktop shortcuts use it.
    /// </summary>
    private static string? FindProfileIcon(BrowserTarget target)
    {
        if (target.Browser.Family != BrowserFamily.Chromium || target.Browser.UserDataPath is null || target.Profile is null)
            return null;

        try
        {
            string directory = Path.Combine(target.Browser.UserDataPath, target.Profile.Id);
            return Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "* Profile.ico").FirstOrDefault() : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private Bitmap? Load(string path, int size, bool isIconFile)
    {
        if (_cache.TryGetValue((path, size), out Bitmap? cached))
            return cached;

        Bitmap? bitmap = null;
        try
        {
            using Icon? icon = isIconFile ? new Icon(path, size, size) : Icon.ExtractIcon(path, 0, size);
            bitmap = icon?.ToBitmap();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or Win32Exception or ExternalException)
        {
            logger.LogDebug(ex, "Could not load an icon from {Path}", path);
        }

        _cache[(path, size)] = bitmap;
        return bitmap;
    }
}
