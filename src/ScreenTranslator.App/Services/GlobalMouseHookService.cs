using System.Runtime.InteropServices;
using ScreenTranslator.Core.Models;

namespace ScreenTranslator.App.Services;

public class GlobalMouseHookService : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc callback, IntPtr hInstance, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_XBUTTONDOWN = 0x020B;
    private const int WM_XBUTTONUP = 0x020C;
    private const int XBUTTON1 = 1;
    private const int XBUTTON2 = 2;

    private const uint MOUSEEVENTF_XDOWN = 0x0080;
    private const uint MOUSEEVENTF_XUP = 0x0100;

    private IntPtr _hookId = IntPtr.Zero;
    private readonly LowLevelMouseProc _hookProc;
    private GestureRecognizer? _recognizer;
    private bool _tracking;
    private bool _reinjecting;
    private readonly Func<GestureConfig> _getConfig;

    public bool Paused { get; set; }

    public event Action<double, double>? GestureStarted;
    public event Action<double, double>? GesturePointAdded;
    public event Action<ScreenRegion>? GestureCompleted;
    public event Action? GestureCancelled;

    public GlobalMouseHookService(Func<GestureConfig> getConfig)
    {
        _getConfig = getConfig;
        // Must keep delegate alive to prevent GC
        _hookProc = HookCallback;
    }

    public void Install()
    {
        if (_hookId != IntPtr.Zero) return;
        var moduleHandle = GetModuleHandle(null);
        _hookId = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, moduleHandle, 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
            return CallNextHookEx(_hookId, nCode, wParam, lParam);

        // Pass through our own re-injected events
        if (_reinjecting)
            return CallNextHookEx(_hookId, nCode, wParam, lParam);

        if (Paused)
            return CallNextHookEx(_hookId, nCode, wParam, lParam);

        var config = _getConfig();
        if (!config.Enabled)
            return CallNextHookEx(_hookId, nCode, wParam, lParam);

        var msg = wParam.ToInt32();
        var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

        if (msg == WM_XBUTTONDOWN)
        {
            var xButton = (int)((hookStruct.mouseData >> 16) & 0xFFFF);
            if (xButton == config.MouseButton)
            {
                _recognizer = new GestureRecognizer();
                _recognizer.AddPoint(hookStruct.pt.x, hookStruct.pt.y);
                _tracking = true;
                GestureStarted?.Invoke(hookStruct.pt.x, hookStruct.pt.y);
                return (IntPtr)1; // suppress native behavior
            }
        }
        else if (msg == WM_XBUTTONUP && _tracking)
        {
            var xButton = (int)((hookStruct.mouseData >> 16) & 0xFFFF);
            if (xButton == config.MouseButton)
            {
                _tracking = false;
                if (_recognizer is not null && _recognizer.IsClosedShape())
                {
                    var region = _recognizer.GetBoundingBox();
                    _recognizer = null;
                    GestureCompleted?.Invoke(region);
                    return (IntPtr)1; // suppress — gesture recognized
                }

                // Gesture not recognized — re-inject the original click
                _recognizer = null;
                GestureCancelled?.Invoke();

                _reinjecting = true;
                var xData = (uint)config.MouseButton;
                mouse_event(MOUSEEVENTF_XDOWN, 0, 0, xData, UIntPtr.Zero);
                mouse_event(MOUSEEVENTF_XUP, 0, 0, xData, UIntPtr.Zero);
                _reinjecting = false;

                return (IntPtr)1; // suppress the original up (re-injected pair replaces it)
            }
        }
        else if (msg == WM_MOUSEMOVE && _tracking && _recognizer is not null)
        {
            _recognizer.AddPoint(hookStruct.pt.x, hookStruct.pt.y);
            GesturePointAdded?.Invoke(hookStruct.pt.x, hookStruct.pt.y);
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }
}
