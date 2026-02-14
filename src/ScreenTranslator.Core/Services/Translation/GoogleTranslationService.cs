using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Core.Services.Translation;

public class GoogleTranslationService : ITranslationService
{
    private readonly HttpClient _httpClient = new();

    public string ProviderName => "Google";

    public async Task<TranslationResult> TranslateAsync(string text, string sourceLang, string targetLang)
    {
        var encoded = HttpUtility.UrlEncode(text);
        var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLang}&tl={targetLang}&dt=t&q={encoded}";

        var response = await _httpClient.GetStringAsync(url);
        var json = JsonDocument.Parse(response);

        var translated = string.Join("",
            json.RootElement[0]
                .EnumerateArray()
                .Select(segment => segment[0].GetString() ?? ""));

        return new TranslationResult(
            OriginalText: text,
            TranslatedText: translated,
            SourceLanguage: sourceLang,
            TargetLanguage: targetLang,
            Provider: ProviderName,
            Timestamp: DateTime.UtcNow);
    }
}
