namespace ScreenTranslator.Core.Models;

public record TranslationResult(
    string OriginalText,
    string TranslatedText,
    string SourceLanguage,
    string TargetLanguage,
    string Provider,
    DateTime Timestamp
);
