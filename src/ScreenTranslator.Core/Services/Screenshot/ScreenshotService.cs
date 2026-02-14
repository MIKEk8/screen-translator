using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Core.Services.Screenshot;

public partial class ScreenshotService : IScreenshotService
{
    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    public ScreenshotResult CaptureRegion(ScreenRegion region)
    {
        using var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(region.X, region.Y, 0, 0, new Size(region.Width, region.Height));

        return new ScreenshotResult(
            ImageData: BitmapToPng(bitmap),
            Width: region.Width,
            Height: region.Height,
            Region: region);
    }

    public ScreenshotResult CaptureFullScreen()
    {
        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);
        var region = new ScreenRegion(0, 0, width, height);
        return CaptureRegion(region);
    }

    private static byte[] BitmapToPng(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
