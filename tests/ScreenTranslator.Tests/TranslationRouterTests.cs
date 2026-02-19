using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;
using ScreenTranslator.Core.Services.Translation;

namespace ScreenTranslator.Tests;

public class TranslationRouterTests
{
    private class FakeConfigService : IConfigService
    {
        public AppConfig Config { get; set; } = new();
        public event Action<AppConfig>? ConfigChanged;
        public Task SaveAsync() { ConfigChanged?.Invoke(Config); return Task.CompletedTask; }
        public Task LoadAsync() => Task.CompletedTask;
    }

    private class FakeTranslationService(string name) : ITranslationService
    {
        public string ProviderName => name;
        public Task<TranslationResult> TranslateAsync(string text, string sourceLang, string targetLang)
            => Task.FromResult(new TranslationResult(text, $"translated:{text}", sourceLang, targetLang, name, DateTime.UtcNow));
        public Task<TranslationResult> TranslateImageAsync(byte[] imageData, string sourceLang, string targetLang)
            => Task.FromResult(new TranslationResult("", "image-translated", sourceLang, targetLang, name, DateTime.UtcNow));
    }

    [Fact]
    public void ProviderName_Google_ReturnsGoogleFactory()
    {
        var config = new FakeConfigService();
        config.Config.TranslationProvider = TranslationProvider.Google;

        var router = new TranslationRouter(
            config,
            _ => new FakeTranslationService("OpenAI"),
            () => new FakeTranslationService("Google"));

        Assert.Equal("Google", router.ProviderName);
    }

    [Fact]
    public void ProviderName_OpenAi_ReturnsOpenAiFactory()
    {
        var config = new FakeConfigService();
        config.Config.TranslationProvider = TranslationProvider.OpenAiCompatible;

        var router = new TranslationRouter(
            config,
            preset => new FakeTranslationService($"OpenAI:{preset.Model}"),
            () => new FakeTranslationService("Google"));

        Assert.StartsWith("OpenAI:", router.ProviderName);
    }

    [Fact]
    public void Google_IsCached_ReturnsSameInstance()
    {
        var config = new FakeConfigService();
        config.Config.TranslationProvider = TranslationProvider.Google;

        int callCount = 0;
        var router = new TranslationRouter(
            config,
            _ => new FakeTranslationService("OpenAI"),
            () => { callCount++; return new FakeTranslationService("Google"); });

        _ = router.ProviderName;
        _ = router.ProviderName;

        Assert.Equal(1, callCount); // cached — factory called once
    }

    [Fact]
    public void OpenAi_IsNotCached_CreatesNewEachTime()
    {
        var config = new FakeConfigService();
        config.Config.TranslationProvider = TranslationProvider.OpenAiCompatible;

        int callCount = 0;
        var router = new TranslationRouter(
            config,
            _ => { callCount++; return new FakeTranslationService("OpenAI"); },
            () => new FakeTranslationService("Google"));

        _ = router.ProviderName;
        _ = router.ProviderName;

        Assert.Equal(2, callCount); // not cached — factory called each time
    }
}
