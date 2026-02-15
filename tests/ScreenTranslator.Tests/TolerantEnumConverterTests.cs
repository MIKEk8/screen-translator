using System.Text.Json;
using ScreenTranslator.Core.Models;

namespace ScreenTranslator.Tests;

public class TolerantEnumConverterTests
{
    [Fact]
    public void Read_ValidString_ParsesCorrectly()
    {
        var json = """{"OcrEngine":"Tesseract"}""";
        var result = JsonSerializer.Deserialize<TestConfig>(json);

        Assert.Equal(OcrEngine.Tesseract, result!.OcrEngine);
    }

    [Fact]
    public void Read_ValidString_CaseInsensitive()
    {
        var json = """{"OcrEngine":"tesseract"}""";
        var result = JsonSerializer.Deserialize<TestConfig>(json);

        Assert.Equal(OcrEngine.Tesseract, result!.OcrEngine);
    }

    [Fact]
    public void Read_ValidInteger_ParsesCorrectly()
    {
        var json = """{"OcrEngine":1}""";
        var result = JsonSerializer.Deserialize<TestConfig>(json);

        Assert.Equal(OcrEngine.Tesseract, result!.OcrEngine);
    }

    [Fact]
    public void Read_UnknownString_ReturnsDefault()
    {
        var json = """{"OcrEngine":"DeletedEngine"}""";
        var result = JsonSerializer.Deserialize<TestConfig>(json);

        Assert.Equal(default(OcrEngine), result!.OcrEngine);
    }

    [Fact]
    public void Read_UndefinedInteger_ReturnsDefault()
    {
        var json = """{"OcrEngine":999}""";
        var result = JsonSerializer.Deserialize<TestConfig>(json);

        Assert.Equal(default(OcrEngine), result!.OcrEngine);
    }

    [Fact]
    public void Write_SerializesAsString()
    {
        var config = new TestConfig { OcrEngine = OcrEngine.Vision };
        var json = JsonSerializer.Serialize(config);

        Assert.Contains("\"Vision\"", json);
    }

    [Fact]
    public void Read_TranslationProvider_ValidString()
    {
        var json = """{"TranslationProvider":"Ollama"}""";
        var result = JsonSerializer.Deserialize<TestConfig2>(json);

        Assert.Equal(TranslationProvider.Ollama, result!.TranslationProvider);
    }

    [Fact]
    public void Read_TranslationProvider_UnknownString_ReturnsDefault()
    {
        var json = """{"TranslationProvider":"DeepL"}""";
        var result = JsonSerializer.Deserialize<TestConfig2>(json);

        Assert.Equal(default(TranslationProvider), result!.TranslationProvider);
    }

    [Fact]
    public void FullAppConfig_WithUnknownEnum_DoesNotThrow()
    {
        var json = """
        {
            "sourceLanguage": "en",
            "targetLanguage": "ru",
            "ocrEngine": "FutureEngine",
            "translationProvider": "RemovedProvider"
        }
        """;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var config = JsonSerializer.Deserialize<AppConfig>(json, options);

        Assert.NotNull(config);
        Assert.Equal(OcrEngine.WindowsOcr, config.OcrEngine);      // default
        Assert.Equal(TranslationProvider.Google, config.TranslationProvider); // default
        Assert.Equal("en", config.SourceLanguage);
    }

    private class TestConfig
    {
        public OcrEngine OcrEngine { get; set; }
    }

    private class TestConfig2
    {
        public TranslationProvider TranslationProvider { get; set; }
    }
}
