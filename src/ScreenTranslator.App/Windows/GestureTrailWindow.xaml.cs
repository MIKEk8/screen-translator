using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using ScreenTranslator.Core.Services.Screenshot;

namespace ScreenTranslator.App.Windows;

public partial class GestureTrailWindow : Window
{
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    private double _dpiScaleX = 1.0;
    private double _dpiScaleY = 1.0;
    private double _offsetX;
    private double _offsetY;
    private bool _initialized;

    public GestureTrailWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Make window click-through at Win32 level
        var hwnd = new WindowInteropHelper(this).Handle;
        var extStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extStyle | WS_EX_TRANSPARENT);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            _dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
            _dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
        }

        var monitorService = new MultiMonitorService();
        var bounds = monitorService.GetVirtualDesktopBounds();

        Left = bounds.Left / _dpiScaleX;
        Top = bounds.Top / _dpiScaleY;
        Width = bounds.Width / _dpiScaleX;
        Height = bounds.Height / _dpiScaleY;

        _offsetX = bounds.Left;
        _offsetY = bounds.Top;
        _initialized = true;
    }

    /// <summary>
    /// Prepare for a new gesture trail (clear old points).
    /// Window stays shown permanently — no Show()/Hide() needed.
    /// </summary>
    public void ShowTrail()
    {
        TrailLine.Points = new PointCollection();
    }

    public void AddPoint(double physX, double physY)
    {
        if (!_initialized) return;
        var logicalX = (physX - _offsetX) / _dpiScaleX;
        var logicalY = (physY - _offsetY) / _dpiScaleY;
        TrailLine.Points.Add(new Point(logicalX, logicalY));
    }

    /// <summary>
    /// Clear the trail. Window stays shown (transparent + click-through).
    /// </summary>
    public void HideTrail()
    {
        TrailLine.Points = new PointCollection();
    }
}
