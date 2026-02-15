using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;
using Tesseract;

namespace ScreenTranslator.Core.Services.Ocr;

public class TesseractOcrService : IOcrService
{
    private static readonly Dictionary<string, string> LanguageMap = new()
    {
        ["en"] = "eng", ["ru"] = "rus", ["de"] = "deu", ["fr"] = "fra",
        ["es"] = "spa", ["ja"] = "jpn", ["zh"] = "chi_sim", ["ko"] = "kor",
        ["pt"] = "por", ["it"] = "ita", ["ar"] = "ara", ["uk"] = "ukr"
    };

    private readonly string _tessdataPath;
    private TesseractEngine? _engine;
    private string? _engineLanguage;

    public TesseractOcrService(string tessdataPath)
    {
        _tessdataPath = Path.GetFullPath(tessdataPath);
    }

    public IReadOnlyList<string> SupportedLanguages
    {
        get
        {
            if (!Directory.Exists(_tessdataPath)) return [];
            return Directory.GetFiles(_tessdataPath, "*.traineddata")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToList();
        }
    }

    public Task<OcrResult> RecognizeAsync(byte[] imageData, string? language = null)
    {
        var tessLang = MapLanguage(language ?? "en");
        var processed = ImagePreprocessor.PreprocessForTesseract(imageData);

        var engine = GetOrCreateEngine(tessLang);
        using var pix = Pix.LoadFromMemory(processed);
        using var page = engine.Process(pix);

        var text = page.GetText()?.Trim() ?? "";
        var confidence = page.GetMeanConfidence();

        var blocks = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => new OcrTextBlock(line.Trim(), new OcrBoundingBox(0, 0, 0, 0)))
            .ToList();

        return Task.FromResult(new OcrResult(
            Text: text,
            Language: language ?? "auto",
            Confidence: confidence,
            Blocks: blocks));
    }

    private TesseractEngine GetOrCreateEngine(string tessLang)
    {
        if (_engine is not null && _engineLanguage == tessLang)
            return _engine;

        _engine?.Dispose();
        _engine = new TesseractEngine(_tessdataPath, tessLang, EngineMode.Default);
        _engineLanguage = tessLang;
        return _engine;
    }

    private static string MapLanguage(string code)
    {
        return LanguageMap.TryGetValue(code.ToLowerInvariant(), out var tessCode) ? tessCode : code;
    }
}
