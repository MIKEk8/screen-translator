using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenTranslator.Core.Models;

/// <summary>
/// Enum converter that returns default(T) instead of throwing on unknown values.
/// Preserves valid config fields when enum members are renamed/removed.
/// </summary>
public class TolerantEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (value is not null && Enum.TryParse<TEnum>(value, ignoreCase: true, out var result))
                return result;
        }
        else if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var intValue))
        {
            if (Enum.IsDefined(typeof(TEnum), intValue))
                return (TEnum)(object)intValue;
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class AppConfig
{
    public string SourceLanguage { get; set; } = "en";
    public string TargetLanguage { get; set; } = "ru";
    public OcrEngine OcrEngine { get; set; } = OcrEngine.WindowsOcr;
    public TranslationProvider TranslationProvider { get; set; } = TranslationProvider.Google;
    public HotkeyConfig Hotkey { get; set; } = new();
    public OverlayConfig Overlay { get; set; } = new();
    public OllamaConfig Ollama { get; set; } = new();
    public List<OpenAiPreset> OpenAiPresets { get; set; } = [new()];
    public string ActiveOpenAiPreset { get; set; } = "Default";
    public TtsConfig Tts { get; set; } = new();

    public OpenAiPreset GetActivePreset() =>
        OpenAiPresets.FirstOrDefault(p => p.Name == ActiveOpenAiPreset)
        ?? OpenAiPresets.FirstOrDefault()
        ?? new();
    public bool AutoDetectLanguage { get; set; } = true;
}

[JsonConverter(typeof(TolerantEnumConverter<OcrEngine>))]
public enum OcrEngine
{
    WindowsOcr,
    Tesseract
}

[JsonConverter(typeof(TolerantEnumConverter<TranslationProvider>))]
public enum TranslationProvider
{
    Google,
    OpenAiCompatible,
    Ollama
}

public class HotkeyConfig
{
    public string CaptureKey { get; set; } = "Alt+A";
    public string StopSpeechKey { get; set; } = "Alt+X";
    public string CopyTranslateKey { get; set; } = "Alt+C";
}

public class OverlayConfig
{
    public double Opacity { get; set; } = 0.9;
    public int FontSize { get; set; } = 14;
    public bool ShowOnTranslate { get; set; } = true;
}

public class OllamaConfig
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "gemma3:4b";
}

public class OpenAiPreset
{
    public string Name { get; set; } = "Default";
    public string ApiEndpoint { get; set; } = "https://openrouter.ai/api/v1";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o-mini";
    public string SystemPrompt { get; set; } = "";

    public override string ToString() => Name;
}

public class TtsConfig
{
    public string VoiceName { get; set; } = "Irina";
    public int Rate { get; set; } = 8;
    public int Volume { get; set; } = 100;
    public bool AutoSpeakTranslation { get; set; } = true;
}

public static class SupportedLanguages
{
    public static readonly IReadOnlyList<LanguageOption> All =
    [
        new("en", "English"),
        new("ru", "Russian"),
        new("de", "German"),
        new("fr", "French"),
        new("es", "Spanish"),
        new("ja", "Japanese"),
        new("zh", "Chinese"),
        new("ko", "Korean"),
        new("pt", "Portuguese"),
        new("it", "Italian"),
        new("ar", "Arabic"),
        new("uk", "Ukrainian"),
    ];
}

public record LanguageOption(string Code, string Name)
{
    public override string ToString() => $"{Code.ToUpper()} — {Name}";
}
