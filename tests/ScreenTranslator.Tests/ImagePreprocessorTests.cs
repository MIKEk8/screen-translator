using System.Drawing;
using System.Drawing.Imaging;
using ScreenTranslator.Core.Services.Ocr;

namespace ScreenTranslator.Tests;

public class ImagePreprocessorTests
{
    private static byte[] CreateTestPng(int width, int height, Color? color = null)
    {
        using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(color ?? Color.DarkBlue);
        // Draw some "text-like" content
        using var font = new Font("Arial", Math.Max(8, height / 3));
        g.DrawString("Test", font, Brushes.White, 5, 5);
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    [Fact]
    public void SmallImage_IsUpscaledForTesseract()
    {
        var small = CreateTestPng(60, 30);
        var processed = ImagePreprocessor.PreprocessForTesseract(small);

        using var ms = new MemoryStream(processed);
        using var result = new Bitmap(ms);

        // 30px min → scale to 300px (10x, capped at 4x) → 30*4=120 + 2*20 padding = 160
        Assert.True(result.Height > 30, $"Expected height > 30, got {result.Height}");
        Assert.True(result.Width > 60, $"Expected width > 60, got {result.Width}");
    }

    [Fact]
    public void LargeImage_NotUpscaledForWindowsOcr()
    {
        var large = CreateTestPng(800, 400);
        var processed = ImagePreprocessor.PreprocessForWindowsOcr(large);

        // Large image should be returned as-is
        Assert.Equal(large.Length, processed.Length);
    }

    [Fact]
    public void SmallImage_IsUpscaledForWindowsOcr()
    {
        var small = CreateTestPng(60, 40);
        var processed = ImagePreprocessor.PreprocessForWindowsOcr(small);

        using var ms = new MemoryStream(processed);
        using var result = new Bitmap(ms);

        Assert.True(result.Height > 40, $"Expected height > 40, got {result.Height}");
    }

    [Fact]
    public void TesseractPreprocess_ProducesGrayscaleImage()
    {
        var color = CreateTestPng(100, 50, Color.Red);
        var processed = ImagePreprocessor.PreprocessForTesseract(color);

        using var ms = new MemoryStream(processed);
        using var result = new Bitmap(ms);

        // Sample some pixels — after Otsu they should be either black or white
        var pixel = result.GetPixel(result.Width / 2, result.Height / 2);
        Assert.True(pixel.R == pixel.G && pixel.G == pixel.B,
            $"Expected grayscale pixel, got R={pixel.R} G={pixel.G} B={pixel.B}");
        Assert.True(pixel.R == 0 || pixel.R == 255,
            $"Expected binary pixel (0 or 255), got {pixel.R}");
    }

    [Fact]
    public void TesseractPreprocess_AddsPadding()
    {
        var input = CreateTestPng(100, 50);
        var processed = ImagePreprocessor.PreprocessForTesseract(input);

        using var ms = new MemoryStream(processed);
        using var result = new Bitmap(ms);

        // With scale=1.0 (100x50, min=50 < 300 → scale ~4x → 400x200 + 40 padding)
        // Padding: top-left corner should be white
        var corner = result.GetPixel(5, 5);
        Assert.Equal(255, corner.R);
        Assert.Equal(255, corner.G);
        Assert.Equal(255, corner.B);
    }

    [Theory]
    [InlineData(30, 20, 300, 4.0, 4.0)]    // 20 < 300 → 300/20=15, capped at 4.0
    [InlineData(500, 400, 300, 4.0, 1.0)]   // 400 >= 300 → no scale
    [InlineData(100, 100, 200, 3.0, 2.0)]   // 100 < 200 → 200/100=2.0
    public void CalculateScaleFactor_ReturnsCorrectValue(int w, int h, int target, double max, double expected)
    {
        var scale = ImagePreprocessor.CalculateScaleFactor(w, h, target, max);
        Assert.Equal(expected, scale, precision: 1);
    }
}
