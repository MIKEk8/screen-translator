using System.Text.Json;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Config;

namespace ScreenTranslator.Tests;

public class ConfigTests
{
    [Fact]
    public void AppConfig_DefaultValues_AreCorrect()
    {
        var config = new AppConfig();

        Assert.Equal("en", config.SourceLanguage);
        Assert.Equal("ru", config.TargetLanguage);
        Assert.Equal(OcrEngine.WindowsOcr, config.OcrEngine);
        Assert.Equal(TranslationProvider.Google, config.TranslationProvider);
        Assert.Equal("Alt+A", config.Hotkey.CaptureKey);
    }

    [Fact]
    public void AppConfig_SerializesAsJson()
    {
        var config = new AppConfig { SourceLanguage = "ja", TargetLanguage = "en" };
        var json = JsonSerializer.Serialize(config);

        Assert.Contains("ja", json);
        Assert.Contains("en", json);

        var deserialized = JsonSerializer.Deserialize<AppConfig>(json);
        Assert.NotNull(deserialized);
        Assert.Equal("ja", deserialized.SourceLanguage);
    }

    [Fact]
    public async Task JsonConfigService_SaveAndLoad()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"screen_translator_test_{Guid.NewGuid()}.json");
        try
        {
            var service = new JsonConfigService(tempPath);
            service.Config.SourceLanguage = "de";
            await service.SaveAsync();

            var service2 = new JsonConfigService(tempPath);
            await service2.LoadAsync();

            Assert.Equal("de", service2.Config.SourceLanguage);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}

public class ModelTests
{
    [Fact]
    public void OcrResult_Properties()
    {
        var blocks = new List<OcrTextBlock>
        {
            new("line1", new OcrBoundingBox(0, 0, 100, 20))
        };
        var result = new OcrResult("hello", "en", 0.95f, blocks);

        Assert.Equal("hello", result.Text);
        Assert.Equal("en", result.Language);
        Assert.Single(result.Blocks);
    }

    [Fact]
    public void TranslationResult_CreatesCorrectly()
    {
        var result = new TranslationResult(
            "hello", "привет", "en", "ru", "Google", DateTime.UtcNow);

        Assert.Equal("hello", result.OriginalText);
        Assert.Equal("привет", result.TranslatedText);
        Assert.Equal("Google", result.Provider);
    }

    [Fact]
    public void ScreenRegion_RecordEquality()
    {
        var region1 = new ScreenRegion(10, 20, 100, 200);
        var region2 = new ScreenRegion(10, 20, 100, 200);

        Assert.Equal(region1, region2);
    }
}
