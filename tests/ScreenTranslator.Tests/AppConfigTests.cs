using ScreenTranslator.Core.Models;

namespace ScreenTranslator.Tests;

public class AppConfigTests
{
    [Fact]
    public void GetActivePreset_ReturnsMatchingPreset()
    {
        var config = new AppConfig
        {
            OpenAiPresets =
            [
                new OpenAiPreset { Name = "First", Model = "model-a" },
                new OpenAiPreset { Name = "Second", Model = "model-b" }
            ],
            ActiveOpenAiPreset = "Second"
        };

        var preset = config.GetActivePreset();

        Assert.Equal("Second", preset.Name);
        Assert.Equal("model-b", preset.Model);
    }

    [Fact]
    public void GetActivePreset_NoMatch_ReturnsFallback()
    {
        var config = new AppConfig
        {
            OpenAiPresets = [new OpenAiPreset { Name = "Default" }],
            ActiveOpenAiPreset = "Nonexistent"
        };

        var preset = config.GetActivePreset();

        Assert.Equal("Default", preset.Name);
    }

    [Fact]
    public void GetActivePreset_EmptyPresets_ReturnsNewPreset()
    {
        var config = new AppConfig
        {
            OpenAiPresets = [],
            ActiveOpenAiPreset = "Anything"
        };

        var preset = config.GetActivePreset();
        Assert.NotNull(preset);
    }

    [Fact]
    public void DefaultConfig_HasReasonableDefaults()
    {
        var config = new AppConfig();

        Assert.Equal("en", config.SourceLanguage);
        Assert.Equal("ru", config.TargetLanguage);
        Assert.Equal(OcrEngine.WindowsOcr, config.OcrEngine);
        Assert.Equal(TranslationProvider.Google, config.TranslationProvider);
        Assert.True(config.AutoDetectLanguage);
        Assert.Equal("auto", config.InterfaceLanguage);
        Assert.Equal(3, config.OpenAiPresets.Count);
        Assert.Equal("Qwen3 Next 80B", config.ActiveOpenAiPreset);
        Assert.Equal("Alt+A", config.Hotkey.CaptureKey);
        Assert.Equal("Alt+C", config.Hotkey.CopyTranslateKey);
        Assert.Equal("Alt+X", config.Hotkey.StopSpeechKey);
        Assert.Equal("Alt+S", config.Hotkey.ScreenshotKey);
        Assert.Equal(0.9, config.Overlay.Opacity);
        Assert.True(config.Overlay.ShowOnTranslate);
        Assert.True(config.Gesture.Enabled);
    }

    [Fact]
    public void SupportedLanguages_ContainsExpected()
    {
        Assert.Contains(SupportedLanguages.All, l => l.Code == "en");
        Assert.Contains(SupportedLanguages.All, l => l.Code == "ru");
        Assert.Contains(SupportedLanguages.All, l => l.Code == "ja");
        Assert.Contains(SupportedLanguages.All, l => l.Code == "zh");
        Assert.True(SupportedLanguages.All.Count >= 10);
    }

    [Fact]
    public void LanguageOption_ToString_FormatsCorrectly()
    {
        var lang = new LanguageOption("en", "English");

        Assert.Equal("EN — English", lang.ToString());
    }

    [Fact]
    public void OpenAiPreset_ToString_ReturnsName()
    {
        var preset = new OpenAiPreset { Name = "MyPreset" };

        Assert.Equal("MyPreset", preset.ToString());
    }

    [Fact]
    public void TtsConfig_Defaults()
    {
        var tts = new TtsConfig();

        Assert.Equal(TtsProvider.Sapi5, tts.Provider);
        Assert.Equal(8, tts.Rate);
        Assert.Equal(100, tts.Volume);
        Assert.True(tts.AutoSpeakTranslation);
        Assert.Equal("Qwen3-TTS", tts.TtsPresetName);
        Assert.Equal("qwen/qwen3-tts", tts.TtsModel);
        Assert.Equal("alloy", tts.TtsVoice);
    }

    [Fact]
    public void GetTtsPreset_ReturnsMatchingPreset()
    {
        var config = new AppConfig();
        config.Tts.TtsPresetName = "Qwen3-TTS";

        var preset = config.GetTtsPreset();

        Assert.Equal("Qwen3-TTS", preset.Name);
    }

    [Fact]
    public void GetTtsPreset_NoMatch_ReturnsFallback()
    {
        var config = new AppConfig();
        config.Tts.TtsPresetName = "Nonexistent";

        var preset = config.GetTtsPreset();

        Assert.NotNull(preset);
    }

    [Fact]
    public void GestureConfig_Defaults()
    {
        var gesture = new GestureConfig();

        Assert.True(gesture.Enabled);
        Assert.Equal(2, gesture.MouseButton); // XButton2
    }

    [Fact]
    public void TesseractOcrConfig_DefaultPath()
    {
        var tess = new TesseractOcrConfig();

        Assert.Equal("tessdata", tess.TessdataPath);
    }
}
