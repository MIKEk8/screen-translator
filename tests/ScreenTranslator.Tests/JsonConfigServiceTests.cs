using System.Text.Json;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Config;

namespace ScreenTranslator.Tests;

public class JsonConfigServiceTests : IDisposable
{
    private readonly string _tempDir;

    public JsonConfigServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"config_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string TempPath(string name = "config.json") => Path.Combine(_tempDir, name);

    [Fact]
    public async Task Load_MissingFile_CreatesDefaults()
    {
        var path = TempPath();
        var svc = new JsonConfigService(path);

        await svc.LoadAsync();

        Assert.True(File.Exists(path));
        Assert.Equal("en", svc.Config.SourceLanguage);
    }

    [Fact]
    public async Task SaveAndLoad_PreservesValues()
    {
        var path = TempPath();
        var svc = new JsonConfigService(path);
        svc.Config.SourceLanguage = "ja";
        svc.Config.TargetLanguage = "en";
        svc.Config.OcrEngine = OcrEngine.Tesseract;
        svc.Config.TranslationProvider = TranslationProvider.Ollama;
        await svc.SaveAsync();

        var svc2 = new JsonConfigService(path);
        await svc2.LoadAsync();

        Assert.Equal("ja", svc2.Config.SourceLanguage);
        Assert.Equal("en", svc2.Config.TargetLanguage);
        Assert.Equal(OcrEngine.Tesseract, svc2.Config.OcrEngine);
        Assert.Equal(TranslationProvider.Ollama, svc2.Config.TranslationProvider);
    }

    [Fact]
    public async Task Load_CorruptedJson_ResetsToDefaults()
    {
        var path = TempPath();
        await File.WriteAllTextAsync(path, "THIS IS NOT JSON {{{");

        var svc = new JsonConfigService(path);
        await svc.LoadAsync();

        Assert.Equal("en", svc.Config.SourceLanguage);
        Assert.Equal(TranslationProvider.Google, svc.Config.TranslationProvider);
    }

    [Fact]
    public async Task Load_UnknownEnumValues_HandledGracefully()
    {
        var path = TempPath();
        var json = """
        {
            "sourceLanguage": "de",
            "ocrEngine": "FutureEngine",
            "translationProvider": "DeepL"
        }
        """;
        await File.WriteAllTextAsync(path, json);

        var svc = new JsonConfigService(path);
        await svc.LoadAsync();

        Assert.Equal("de", svc.Config.SourceLanguage);
        Assert.Equal(OcrEngine.WindowsOcr, svc.Config.OcrEngine);            // fallback
        Assert.Equal(TranslationProvider.Google, svc.Config.TranslationProvider); // fallback
    }

    [Fact]
    public async Task Load_ExtraFields_IgnoredGracefully()
    {
        var path = TempPath();
        var json = """
        {
            "sourceLanguage": "fr",
            "futureFeature": true,
            "nestedFuture": { "x": 1 }
        }
        """;
        await File.WriteAllTextAsync(path, json);

        var svc = new JsonConfigService(path);
        await svc.LoadAsync();

        Assert.Equal("fr", svc.Config.SourceLanguage);
    }

    [Fact]
    public async Task ConfigChanged_FiredOnLoad()
    {
        var path = TempPath();
        var svc = new JsonConfigService(path);

        AppConfig? received = null;
        svc.ConfigChanged += cfg => received = cfg;

        await svc.LoadAsync();

        Assert.NotNull(received);
    }

    [Fact]
    public async Task ConfigChanged_FiredOnSave()
    {
        var path = TempPath();
        var svc = new JsonConfigService(path);

        AppConfig? received = null;
        svc.ConfigChanged += cfg => received = cfg;

        await svc.SaveAsync();

        Assert.NotNull(received);
    }

    [Fact]
    public async Task Save_CreatesDirectoryIfMissing()
    {
        var path = Path.Combine(_tempDir, "subdir", "deep", "config.json");
        var svc = new JsonConfigService(path);

        await svc.SaveAsync();

        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task Save_PreservesPresets()
    {
        var path = TempPath();
        var svc = new JsonConfigService(path);
        svc.Config.OpenAiPresets =
        [
            new OpenAiPreset { Name = "GPT4", ApiEndpoint = "https://api.example.com", Model = "gpt-4" },
            new OpenAiPreset { Name = "Local", ApiEndpoint = "http://localhost:8080", Model = "local-model" }
        ];
        svc.Config.ActiveOpenAiPreset = "Local";
        await svc.SaveAsync();

        var svc2 = new JsonConfigService(path);
        await svc2.LoadAsync();

        Assert.Equal(2, svc2.Config.OpenAiPresets.Count);
        Assert.Equal("Local", svc2.Config.ActiveOpenAiPreset);
        Assert.Equal("local-model", svc2.Config.GetActivePreset().Model);
    }

    [Fact]
    public async Task Save_EncryptsApiKeys_LoadDecrypts()
    {
        var path = TempPath();
        var svc = new JsonConfigService(path);
        svc.Config.OpenAiPresets =
        [
            new OpenAiPreset { Name = "Test", ApiKey = "sk-secret-key-123" }
        ];
        svc.Config.Tts.DeepInfraApiKey = "di-secret-456";
        await svc.SaveAsync();

        // In-memory config should still have plaintext
        Assert.Equal("sk-secret-key-123", svc.Config.OpenAiPresets[0].ApiKey);
        Assert.Equal("di-secret-456", svc.Config.Tts.DeepInfraApiKey);

        // On-disk file should have encrypted values
        var json = File.ReadAllText(path);
        Assert.Contains("enc:", json);
        Assert.DoesNotContain("sk-secret-key-123", json);

        // Load should decrypt back
        var svc2 = new JsonConfigService(path);
        await svc2.LoadAsync();
        Assert.Equal("sk-secret-key-123", svc2.Config.OpenAiPresets[0].ApiKey);
        Assert.Equal("di-secret-456", svc2.Config.Tts.DeepInfraApiKey);
    }

    [Fact]
    public async Task Load_PlaintextApiKeys_MigratesOnSave()
    {
        var path = TempPath();
        // Write config with plaintext API keys (simulating old format)
        var json = """
        {
            "openAiPresets": [
                {
                    "name": "Old",
                    "apiEndpoint": "https://api.example.com",
                    "apiKey": "plain-key-789",
                    "model": "gpt-4"
                }
            ],
            "tts": {
                "deepInfraApiKey": "plain-di-key"
            }
        }
        """;
        await File.WriteAllTextAsync(path, json);

        var svc = new JsonConfigService(path);
        await svc.LoadAsync();

        // Plaintext keys should be readable
        Assert.Equal("plain-key-789", svc.Config.OpenAiPresets[0].ApiKey);
        Assert.Equal("plain-di-key", svc.Config.Tts.DeepInfraApiKey);

        // After save, they should be encrypted on disk
        await svc.SaveAsync();
        var savedJson = File.ReadAllText(path);
        Assert.DoesNotContain("plain-key-789", savedJson);
        Assert.Contains("enc:", savedJson);
    }
}
