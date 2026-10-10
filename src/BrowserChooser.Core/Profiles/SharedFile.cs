namespace BrowserChooser.Core.Profiles;

internal static class SharedFile
{
    /// <summary>
    /// Reads a file that the browser may have open for writing. <see cref="File.ReadAllText(string)"/> would fail
    /// with a sharing violation in that case.
    /// </summary>
    public static string ReadAllText(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
