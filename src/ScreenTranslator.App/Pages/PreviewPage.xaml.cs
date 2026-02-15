using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using ScreenTranslator.App.Windows;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;
using ScreenTranslator.Core.Services.Localization;

namespace ScreenTranslator.App.Pages;

public partial class PreviewPage : Page
{
    private readonly IScreenshotService _screenshotService;
    private readonly IOcrService _ocrService;
    private readonly IConfigService _configService;
    private readonly ILocalizationService _loc;
    private double _zoomLevel = 1.0;

    public PreviewPage()
    {
        InitializeComponent();

        _screenshotService = App.Services.GetRequiredService<IScreenshotService>();
        _ocrService = App.Services.GetRequiredService<IOcrService>();
        _configService = App.Services.GetRequiredService<IConfigService>();
        _loc = App.Services.GetRequiredService<ILocalizationService>();

        ApplyTranslations();
        _loc.LanguageChanged += _ => Dispatcher.Invoke(ApplyTranslations);
    }

    private void ApplyTranslations()
    {
        PreviewTitleText.Text = _loc.T("preview.title");
        CaptureBtn.Content = $"\U0001F4F7  {_loc.T("preview.capture")}";
        NoCaptureText.Text = _loc.T("preview.no_capture");
        CaptureHintText.Text = _loc.T("preview.hint");
        FitBtn.Content = _loc.T("preview.fit");
    }

    public async void CaptureAndPreview()
    {
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow is null) return;

        var prevState = mainWindow.WindowState;
        mainWindow.WindowState = WindowState.Minimized;
        await Task.Delay(200);

        var selector = new AreaSelectorWindow();
        var result = selector.ShowDialog();

        mainWindow.WindowState = prevState;

        if (result == true && selector.SelectedRegion is { } region)
        {
            await ShowCapture(region);
        }
    }

    private async Task ShowCapture(ScreenRegion region)
    {
        try
        {
            var screenshot = _screenshotService.CaptureRegion(region);

            // Show image
            var bitmapImage = new BitmapImage();
            using (var stream = new MemoryStream(screenshot.ImageData))
            {
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            PreviewImage.Source = bitmapImage;
            PlaceholderPanel.Visibility = Visibility.Collapsed;
            ImageScroller.Visibility = Visibility.Visible;

            // Region info
            RegionInfoBadge.Visibility = Visibility.Visible;
            RegionInfoText.Text = $"{region.Width}x{region.Height} @ ({region.X},{region.Y})";

            // Reset zoom
            _zoomLevel = 1.0;
            PreviewImage.Stretch = Stretch.Uniform;
            UpdateZoomText();

            // Run OCR
            OcrResultText.Text = _loc.T("preview.recognizing");
            var ocrResult = await _ocrService.RecognizeAsync(
                screenshot.ImageData,
                _configService.Config.SourceLanguage);

            if (string.IsNullOrWhiteSpace(ocrResult.Text))
            {
                OcrResultText.Text = _loc.T("preview.no_text_ocr");
            }
            else
            {
                var preview = ocrResult.Text.Replace("\n", " ");
                if (preview.Length > 80) preview = preview[..80] + "...";
                OcrResultText.Text = $"OCR ({ocrResult.Language}, {ocrResult.Confidence:P0}): {preview}";
            }
        }
        catch (Exception ex)
        {
            OcrResultText.Text = $"Error: {ex.Message}";
        }
    }

    private void PreviewImage_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (PreviewImage.Source is null) return;

        _zoomLevel += e.Delta > 0 ? 0.1 : -0.1;
        _zoomLevel = Math.Clamp(_zoomLevel, 0.1, 5.0);

        PreviewImage.Stretch = Stretch.None;
        PreviewImage.LayoutTransform = new ScaleTransform(_zoomLevel, _zoomLevel);
        UpdateZoomText();
    }

    private void FitButton_Click(object sender, RoutedEventArgs e)
    {
        _zoomLevel = 1.0;
        PreviewImage.Stretch = Stretch.Uniform;
        PreviewImage.LayoutTransform = Transform.Identity;
        UpdateZoomText();
    }

    private void ActualSizeButton_Click(object sender, RoutedEventArgs e)
    {
        _zoomLevel = 1.0;
        PreviewImage.Stretch = Stretch.None;
        PreviewImage.LayoutTransform = Transform.Identity;
        UpdateZoomText();
    }

    private void UpdateZoomText()
    {
        ZoomText.Text = PreviewImage.Stretch == Stretch.Uniform
            ? _loc.T("preview.fit")
            : $"{_zoomLevel:P0}";
    }

    private void CaptureButton_Click(object sender, RoutedEventArgs e) => CaptureAndPreview();
}
