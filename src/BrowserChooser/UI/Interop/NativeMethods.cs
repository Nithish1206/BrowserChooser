using System.Runtime.InteropServices;

namespace BrowserChooser.UI.Interop;

internal static class NativeMethods
{
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    public const int DWMWA_BORDER_COLOR = 34;
    public const int DWMWCP_ROUND = 2;

    public const int WS_THICKFRAME = 0x00040000;
    public const int WM_NCCALCSIZE = 0x0083;
    public const int WM_NCACTIVATE = 0x0086;
    public const int WM_NCHITTEST = 0x0084;
    public const int HTCLIENT = 1;
    public const int HTCAPTION = 2;
    public const int HTLEFT = 10;
    public const int HTBOTTOMRIGHT = 17;

    /// <summary>Lets any process take the foreground; see <see cref="AllowSetForegroundWindow"/>.</summary>
    public const int ASFW_ANY = -1;

    public const int VK_LBUTTON = 0x01;
    public const int VK_RBUTTON = 0x02;
    public const int VK_MBUTTON = 0x04;

    [DllImport("dwmapi.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

    /// <summary>
    /// Passes this process's right to bring a window to the front on to another process. Only works while this
    /// process is in the foreground, and lasts until the user's next input.
    /// </summary>
    [DllImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AllowSetForegroundWindow(int processId);

    /// <summary>The physical state of a key or mouse button right now, not as of this thread's last message.</summary>
    [DllImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern short GetAsyncKeyState(int virtualKey);

    public static bool IsKeyDown(int virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    /// <summary>Sets a DWM window attribute; returns false where unsupported (for example, Windows 10).</summary>
    public static bool TrySetDwmAttribute(IntPtr hwnd, int attribute, int value) =>
        DwmSetWindowAttribute(hwnd, attribute, ref value, sizeof(int)) == 0;
}
