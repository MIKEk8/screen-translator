using ScreenTranslator.Core.Models;

namespace ScreenTranslator.Core.Services.Interfaces;

public interface IOcrService
{
    Task<OcrResult> RecognizeAsync(byte[] imageData, string? language = null);
    IReadOnlyList<string> SupportedLanguages { get; }
}
