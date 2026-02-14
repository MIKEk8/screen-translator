using System.Runtime.InteropServices;
using ScreenTranslator.Core.Models;

namespace ScreenTranslator.Core.Services.Screenshot;

public class MultiMonitorService
{
    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfoW(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, out uint dpiX, out uint dpiY);

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    public List<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
        {
            var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
            if (GetMonitorInfoW(hMonitor, ref info))
            {
                double scale = 1.0;
                if (GetDpiForMonitor(hMonitor, 0, out uint dpiX, out _) == 0)
                    scale = dpiX / 96.0;

                monitors.Add(new MonitorInfo(
                    DeviceName: info.szDevice,
                    Left: info.rcMonitor.Left,
                    Top: info.rcMonitor.Top,
                    Width: info.rcMonitor.Right - info.rcMonitor.Left,
                    Height: info.rcMonitor.Bottom - info.rcMonitor.Top,
                    IsPrimary: (info.dwFlags & 1) != 0,
                    ScaleFactor: scale));
            }
            return true;
        }, IntPtr.Zero);

        return monitors;
    }

    public VirtualDesktopBounds GetVirtualDesktopBounds()
    {
        return new VirtualDesktopBounds(
            Left: GetSystemMetrics(SM_XVIRTUALSCREEN),
            Top: GetSystemMetrics(SM_YVIRTUALSCREEN),
            Width: GetSystemMetrics(SM_CXVIRTUALSCREEN),
            Height: GetSystemMetrics(SM_CYVIRTUALSCREEN));
    }
}
