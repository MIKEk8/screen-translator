using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Localization;

namespace ScreenTranslator.App.Windows;

public partial class ScreenshotPreviewWindow : Window
{
    public ScreenshotPreviewWindow(ScreenshotResult screenshot)
    {
        InitializeComponent();

        var loc = App.Services.GetRequiredService<ILocalizationService>();
        HintText.Text = loc.T("screenshot.hint");

        // Position window exactly at the monitor where the screenshot was taken
        var region = screenshot.Region;

        Loaded += (_, _) =>
        {
            var source = PresentationSource.FromVisual(this);
            double scaleX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double scaleY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

            Left = region.X / scaleX;
            Top = region.Y / scaleY;
            Width = region.Width / scaleX;
            Height = region.Height / scaleY;
        };

        // Show the screenshot
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = new MemoryStream(screenshot.ImageData);
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        ScreenshotImage.Source = bitmapImage;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }
}
