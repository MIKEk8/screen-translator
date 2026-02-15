using System.Runtime.InteropServices.WindowsRuntime;
using ScreenTranslator.Core.Services.Interfaces;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using WinOcr = Windows.Media.Ocr;

namespace ScreenTranslator.Core.Services.Ocr;

public class WindowsOcrService : IOcrService
{
    public IReadOnlyList<string> SupportedLanguages =>
        WinOcr.OcrEngine.AvailableRecognizerLanguages
            .Select(l => l.LanguageTag)
            .ToList();

    public async Task<Models.OcrResult> RecognizeAsync(byte[] imageData, string? language = null)
    {
        var engine = CreateEngine(language);

        var processed = ImagePreprocessor.PreprocessForWindowsOcr(imageData);

        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(processed.AsBuffer());
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);

        var ocrResult = await engine.RecognizeAsync(softwareBitmap);

        var blocks = ocrResult.Lines
            .Select(line => new Models.OcrTextBlock(
                line.Text,
                new Models.OcrBoundingBox(0, 0, 0, 0)))
            .ToList();

        var fullText = string.Join("\n", ocrResult.Lines.Select(l => l.Text));

        return new Models.OcrResult(
            Text: fullText,
            Language: language ?? "auto",
            Confidence: 1.0f,
            Blocks: blocks);
    }

    private static WinOcr.OcrEngine CreateEngine(string? language)
    {
        if (language is not null)
        {
            var lang = new Language(language);
            var engine = WinOcr.OcrEngine.TryCreateFromLanguage(lang);
            if (engine is not null)
                return engine;
        }

        return WinOcr.OcrEngine.TryCreateFromUserProfileLanguages()
            ?? throw new InvalidOperationException("No OCR language packs installed");
    }
}
