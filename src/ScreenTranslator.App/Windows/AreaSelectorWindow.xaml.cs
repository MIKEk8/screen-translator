using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Localization;
using ScreenTranslator.Core.Services.Screenshot;

namespace ScreenTranslator.App.Windows;

public partial class AreaSelectorWindow : Window
{
    private Point _startPoint;
    private bool _isSelecting;
    private readonly VirtualDesktopBounds _bounds;
    private double _dpiScaleX = 1.0;
    private double _dpiScaleY = 1.0;

    public ScreenRegion? SelectedRegion { get; private set; }

    public AreaSelectorWindow()
    {
        InitializeComponent();

        var loc = App.Services.GetRequiredService<ILocalizationService>();
        InstructionText.Text = loc.T("selector.hint");

        var monitorService = new MultiMonitorService();
        _bounds = monitorService.GetVirtualDesktopBounds(); // physical pixels

        Loaded += OnLoaded;

        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseUp;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Get DPI scale factor (physical pixels / logical units)
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            _dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
            _dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
        }

        // Convert physical pixel bounds to WPF logical units for window positioning
        double logicalLeft = _bounds.Left / _dpiScaleX;
        double logicalTop = _bounds.Top / _dpiScaleY;
        double logicalWidth = _bounds.Width / _dpiScaleX;
        double logicalHeight = _bounds.Height / _dpiScaleY;

        Left = logicalLeft;
        Top = logicalTop;
        Width = logicalWidth;
        Height = logicalHeight;

        // Dark overlay in logical units
        DarkOverlay.Width = logicalWidth;
        DarkOverlay.Height = logicalHeight;

        // Hide the 4-region overlays initially
        TopRegion.Visibility = Visibility.Collapsed;
        BottomRegion.Visibility = Visibility.Collapsed;
        LeftRegion.Visibility = Visibility.Collapsed;
        RightRegion.Visibility = Visibility.Collapsed;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(OverlayCanvas);
        _isSelecting = true;

        // Switch from single overlay to 4-region mode
        DarkOverlay.Visibility = Visibility.Collapsed;
        TopRegion.Visibility = Visibility.Visible;
        BottomRegion.Visibility = Visibility.Visible;
        LeftRegion.Visibility = Visibility.Visible;
        RightRegion.Visibility = Visibility.Visible;

        SelectionRect.Visibility = Visibility.Visible;
        SizeLabel.Visibility = Visibility.Visible;
        InstructionText.Visibility = Visibility.Collapsed;

        OverlayCanvas.CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSelecting) return;

        var currentPoint = e.GetPosition(OverlayCanvas);
        UpdateSelection(_startPoint, currentPoint);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting) return;
        _isSelecting = false;
        OverlayCanvas.ReleaseMouseCapture();

        var endPoint = e.GetPosition(OverlayCanvas);
        var rect = GetNormalizedRect(_startPoint, endPoint);

        // Minimum size check (10x10 pixels)
        if (rect.Width < 10 || rect.Height < 10)
        {
            SelectedRegion = null;
            DialogResult = false;
            Close();
            return;
        }

        // Convert WPF logical coordinates to physical pixels for CopyFromScreen
        SelectedRegion = new ScreenRegion(
            X: (int)(rect.X * _dpiScaleX) + _bounds.Left,
            Y: (int)(rect.Y * _dpiScaleY) + _bounds.Top,
            Width: (int)(rect.Width * _dpiScaleX),
            Height: (int)(rect.Height * _dpiScaleY));

        DialogResult = true;
        Close();
    }

    private void UpdateSelection(Point start, Point current)
    {
        var rect = GetNormalizedRect(start, current);
        double canvasW = OverlayCanvas.ActualWidth;
        double canvasH = OverlayCanvas.ActualHeight;

        // Selection rectangle
        Canvas.SetLeft(SelectionRect, rect.X);
        Canvas.SetTop(SelectionRect, rect.Y);
        SelectionRect.Width = Math.Max(1, rect.Width);
        SelectionRect.Height = Math.Max(1, rect.Height);

        // Top dark region (above selection)
        Canvas.SetLeft(TopRegion, 0);
        Canvas.SetTop(TopRegion, 0);
        TopRegion.Width = canvasW;
        TopRegion.Height = Math.Max(0, rect.Y);

        // Bottom dark region (below selection)
        Canvas.SetLeft(BottomRegion, 0);
        Canvas.SetTop(BottomRegion, rect.Y + rect.Height);
        BottomRegion.Width = canvasW;
        BottomRegion.Height = Math.Max(0, canvasH - rect.Y - rect.Height);

        // Left dark region (left of selection, between top and bottom)
        Canvas.SetLeft(LeftRegion, 0);
        Canvas.SetTop(LeftRegion, rect.Y);
        LeftRegion.Width = Math.Max(0, rect.X);
        LeftRegion.Height = Math.Max(0, rect.Height);

        // Right dark region (right of selection, between top and bottom)
        Canvas.SetLeft(RightRegion, rect.X + rect.Width);
        Canvas.SetTop(RightRegion, rect.Y);
        RightRegion.Width = Math.Max(0, canvasW - rect.X - rect.Width);
        RightRegion.Height = Math.Max(0, rect.Height);

        // Size label — show physical pixel dimensions
        SizeLabelText.Text = $"{(int)(rect.Width * _dpiScaleX)} x {(int)(rect.Height * _dpiScaleY)}";
        Canvas.SetLeft(SizeLabel, rect.X);
        Canvas.SetTop(SizeLabel, rect.Y + rect.Height + 4);
    }

    private static Rect GetNormalizedRect(Point p1, Point p2)
    {
        return new Rect(
            Math.Min(p1.X, p2.X),
            Math.Min(p1.Y, p2.Y),
            Math.Abs(p2.X - p1.X),
            Math.Abs(p2.Y - p1.Y));
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SelectedRegion = null;
            DialogResult = false;
            Close();
        }
    }
}
