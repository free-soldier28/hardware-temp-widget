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

    private const uint SpiGetWorkArea = 0x0030;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
