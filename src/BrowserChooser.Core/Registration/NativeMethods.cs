using System.Runtime.InteropServices;

namespace BrowserChooser.Core.Registration;

internal static class NativeMethods
{
    /// <summary>File type associations changed.</summary>
    public const int SHCNE_ASSOCCHANGED = 0x08000000;

    public const uint SHCNF_IDLIST = 0x0000;

    [DllImport("shell32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
