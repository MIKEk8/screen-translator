using ScreenTranslator.Core.Models;

namespace ScreenTranslator.Core.Services.Interfaces;

public interface ITranslationService
{
    Task<TranslationResult> TranslateAsync(string text, string sourceLang, string targetLang);
    Task<TranslationResult> TranslateImageAsync(byte[] imageData, string sourceLang, string targetLang);
    string ProviderName { get; }
}
