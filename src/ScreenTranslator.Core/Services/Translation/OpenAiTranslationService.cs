using System.Text;
using System.Text.Json;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Core.Services.Translation;

public class OpenAiTranslationService : ITranslationService
{
    private readonly HttpClient _httpClient = new();
    private readonly OpenAiPreset _config;

    public string ProviderName => "OpenAI";

    public OpenAiTranslationService(OpenAiPreset config)
    {
        _config = config;
    }

    public async Task<TranslationResult> TranslateAsync(string text, string sourceLang, string targetLang)
    {
        if (string.IsNullOrWhiteSpace(_config.ApiKey))
            throw new InvalidOperationException("OpenAI API key is not configured. Set it in Settings.");

        var endpoint = _config.ApiEndpoint.TrimEnd('/');
        var url = $"{endpoint}/chat/completions";

        var systemPrompt = string.IsNullOrWhiteSpace(_config.SystemPrompt)
            ? $"Translate the following text from {sourceLang} to {targetLang}. Return ONLY the translated text, nothing else. Do not add explanations or notes."
            : _config.SystemPrompt
                .Replace("{source}", sourceLang)
                .Replace("{target}", targetLang);

        var requestBody = new
        {
            model = _config.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = text }
            },
            temperature = 0.3
        };

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_config.ApiKey}");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"OpenAI API error ({response.StatusCode}): {responseBody}");

        var doc = JsonDocument.Parse(responseBody);
        var translated = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()?.Trim() ?? "";

        return new TranslationResult(
            OriginalText: text,
            TranslatedText: translated,
            SourceLanguage: sourceLang,
            TargetLanguage: targetLang,
            Provider: $"{ProviderName} ({_config.Model})",
            Timestamp: DateTime.UtcNow);
    }
}
