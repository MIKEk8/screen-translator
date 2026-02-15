using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Core.Services.Ocr;

public class OcrRouter : IOcrService
{
    private readonly IConfigService _configService;
    private readonly WindowsOcrService _windowsOcr = new();
    private TesseractOcrService? _tesseractOcr;
    private string? _tessdataPath;

    public OcrRouter(IConfigService configService)
    {
        _configService = configService;
    }

    public IReadOnlyList<string> SupportedLanguages =>
        GetCurrentEngine().SupportedLanguages;

    public Task<OcrResult> RecognizeAsync(byte[] imageData, string? language = null)
    {
        return GetCurrentEngine().RecognizeAsync(imageData, language);
    }

    private IOcrService GetCurrentEngine()
    {
        return _configService.Config.OcrEngine switch
        {
            OcrEngine.Tesseract => GetTesseract(),
            _ => _windowsOcr
        };
    }

    private TesseractOcrService GetTesseract()
    {
        var path = _configService.Config.TesseractOcr.TessdataPath;
        if (_tesseractOcr is null || _tessdataPath != path)
        {
            _tesseractOcr = new TesseractOcrService(path);
            _tessdataPath = path;
        }
        return _tesseractOcr;
    }
}
