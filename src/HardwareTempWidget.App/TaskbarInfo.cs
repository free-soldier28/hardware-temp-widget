using System.Runtime.InteropServices;
using Avalonia;

namespace HardwareTempWidget.App;

internal static class TaskbarInfo
{
    public static PixelRect? GetTaskbarBounds()
    {
        var taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero || !GetWindowRect(taskbar, out var rect))
        {
            return null;
        }

        return ToPixelRect(rect);
    }

    public static PixelRect? GetTrayNotifyBounds()
    {
        var taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero)
        {
            return null;
        }

        var trayNotify = FindWindowEx(taskbar, IntPtr.Zero, "TrayNotifyWnd", null);
        if (trayNotify == IntPtr.Zero || !GetWindowRect(trayNotify, out var rect))
        {
            return null;
        }

        return ToPixelRect(rect);
    }

    public static PixelRect GetPrimaryWorkArea()
    {
        var rect = default(RECT);
        SystemParametersInfo(SpiGetWorkArea, 0, ref rect, 0);
        return ToPixelRect(rect);
    }

    private static PixelRect ToPixelRect(RECT rect) =>
        new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

    /// Explorer re-asserts the taskbar's own topmost band whenever tray flyouts
    /// (systray overflow, action center, etc.) open, which can bump other topmost
    /// windows behind it. Re-issuing HWND_TOPMOST periodically restores our z-order.
    public static void ForceTopmost(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            return;
        }

        SetWindowPos(hWnd, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
    }

    private const uint SpiGetWorkArea = 0x0030;

    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
