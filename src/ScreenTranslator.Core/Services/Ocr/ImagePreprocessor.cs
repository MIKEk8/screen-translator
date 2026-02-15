using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ScreenTranslator.Core.Services.Ocr;

public static class ImagePreprocessor
{
    /// <summary>
    /// Preprocess image for Tesseract: upscale + padding + grayscale + Otsu binarization.
    /// </summary>
    public static byte[] PreprocessForTesseract(byte[] pngData)
    {
        using var ms = new MemoryStream(pngData);
        using var original = new Bitmap(ms);

        var scale = CalculateScaleFactor(original.Width, original.Height, targetMinDimension: 300, maxScale: 4.0);
        const int padding = 20;

        var scaledW = (int)(original.Width * scale);
        var scaledH = (int)(original.Height * scale);

        using var result = new Bitmap(scaledW + padding * 2, scaledH + padding * 2, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(result))
        {
            g.Clear(Color.White);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(original, padding, padding, scaledW, scaledH);
        }

        ApplyGrayscale(result);
        ApplyOtsuThreshold(result);

        return BitmapToPng(result);
    }

    /// <summary>
    /// Preprocess image for Windows OCR: upscale small images + padding. No binarization.
    /// </summary>
    public static byte[] PreprocessForWindowsOcr(byte[] pngData)
    {
        using var ms = new MemoryStream(pngData);
        using var original = new Bitmap(ms);

        var scale = CalculateScaleFactor(original.Width, original.Height, targetMinDimension: 200, maxScale: 3.0);

        // Skip preprocessing if image is large enough
        if (scale <= 1.0)
            return pngData;

        const int padding = 10;
        var scaledW = (int)(original.Width * scale);
        var scaledH = (int)(original.Height * scale);

        using var result = new Bitmap(scaledW + padding * 2, scaledH + padding * 2, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(result))
        {
            g.Clear(Color.White);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(original, padding, padding, scaledW, scaledH);
        }

        return BitmapToPng(result);
    }

    public static double CalculateScaleFactor(int width, int height, int targetMinDimension, double maxScale)
    {
        var minDimension = Math.Min(width, height);
        if (minDimension >= targetMinDimension) return 1.0;
        return Math.Min(maxScale, (double)targetMinDimension / minDimension);
    }

    private static void ApplyGrayscale(Bitmap bmp)
    {
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        try
        {
            var stride = data.Stride;
            var bytesPerRow = bmp.Width * 3;
            var byteCount = stride * bmp.Height;
            var pixels = new byte[byteCount];
            Marshal.Copy(data.Scan0, pixels, 0, byteCount);

            for (var y = 0; y < bmp.Height; y++)
            {
                var rowOffset = y * stride;
                for (var x = 0; x < bytesPerRow; x += 3)
                {
                    var i = rowOffset + x;
                    // ITU-R BT.601 luminance
                    var gray = (byte)(0.299 * pixels[i + 2] + 0.587 * pixels[i + 1] + 0.114 * pixels[i]);
                    pixels[i] = gray;     // B
                    pixels[i + 1] = gray; // G
                    pixels[i + 2] = gray; // R
                }
            }

            Marshal.Copy(pixels, 0, data.Scan0, byteCount);
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }

    private static void ApplyOtsuThreshold(Bitmap bmp)
    {
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        try
        {
            var stride = data.Stride;
            var bytesPerRow = bmp.Width * 3;
            var byteCount = stride * bmp.Height;
            var pixels = new byte[byteCount];
            Marshal.Copy(data.Scan0, pixels, 0, byteCount);

            // Build histogram (image is already grayscale, use B channel)
            var histogram = new int[256];
            var totalPixels = 0;
            for (var y = 0; y < bmp.Height; y++)
            {
                var rowOffset = y * stride;
                for (var x = 0; x < bytesPerRow; x += 3)
                {
                    histogram[pixels[rowOffset + x]]++;
                    totalPixels++;
                }
            }

            // Otsu's method: find threshold that minimizes intra-class variance
            var threshold = ComputeOtsuThreshold(histogram, totalPixels);

            // Apply threshold
            for (var y = 0; y < bmp.Height; y++)
            {
                var rowOffset = y * stride;
                for (var x = 0; x < bytesPerRow; x += 3)
                {
                    var i = rowOffset + x;
                    var val = pixels[i] > threshold ? (byte)255 : (byte)0;
                    pixels[i] = val;
                    pixels[i + 1] = val;
                    pixels[i + 2] = val;
                }
            }

            Marshal.Copy(pixels, 0, data.Scan0, byteCount);
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }

    private static int ComputeOtsuThreshold(int[] histogram, int totalPixels)
    {
        var sum = 0.0;
        for (var i = 0; i < 256; i++)
            sum += i * histogram[i];

        var sumB = 0.0;
        var wB = 0;
        var maxVariance = 0.0;
        var threshold = 0;

        for (var t = 0; t < 256; t++)
        {
            wB += histogram[t];
            if (wB == 0) continue;

            var wF = totalPixels - wB;
            if (wF == 0) break;

            sumB += t * histogram[t];
            var meanB = sumB / wB;
            var meanF = (sum - sumB) / wF;
            var variance = (double)wB * wF * (meanB - meanF) * (meanB - meanF);

            if (variance > maxVariance)
            {
                maxVariance = variance;
                threshold = t;
            }
        }

        return threshold;
    }

    private static byte[] BitmapToPng(Bitmap bmp)
    {
        using var output = new MemoryStream();
        bmp.Save(output, ImageFormat.Png);
        return output.ToArray();
    }
}
