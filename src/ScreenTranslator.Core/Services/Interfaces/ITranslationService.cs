using ScreenTranslator.Core.Models;

namespace ScreenTranslator.Core.Services.Interfaces;

public interface ITranslationService
{
    Task<TranslationResult> TranslateAsync(string text, string sourceLang, string targetLang);
    string ProviderName { get; }
}
