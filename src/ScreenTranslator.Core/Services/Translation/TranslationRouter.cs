using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Core.Services.Translation;

public class TranslationRouter : ITranslationService
{
    private readonly IConfigService _configService;
    private readonly Func<OpenAiPreset, ITranslationService> _openAiFactory;
    private readonly Func<ITranslationService> _googleFactory;
    private readonly Dictionary<TranslationProvider, ITranslationService> _providers = [];

    public string ProviderName => GetCurrentProvider().ProviderName;

    public TranslationRouter(
        IConfigService configService,
        Func<OpenAiPreset, ITranslationService> openAiFactory,
        Func<ITranslationService> googleFactory)
    {
        _configService = configService;
        _openAiFactory = openAiFactory;
        _googleFactory = googleFactory;
    }

    public Task<TranslationResult> TranslateAsync(string text, string sourceLang, string targetLang)
    {
        return GetCurrentProvider().TranslateAsync(text, sourceLang, targetLang);
    }

    public Task<TranslationResult> TranslateImageAsync(byte[] imageData, string sourceLang, string targetLang)
    {
        return GetCurrentProvider().TranslateImageAsync(imageData, sourceLang, targetLang);
    }

    private ITranslationService GetCurrentProvider()
    {
        var provider = _configService.Config.TranslationProvider;

        // OpenAI: always create fresh — active preset may have changed
        if (provider is TranslationProvider.OpenAiCompatible or TranslationProvider.Ollama)
            return CreateProvider(provider);

        if (!_providers.TryGetValue(provider, out var service))
        {
            service = CreateProvider(provider);
            _providers[provider] = service;
        }

        return service;
    }

    private ITranslationService CreateProvider(TranslationProvider provider) => provider switch
    {
        TranslationProvider.Google => _googleFactory(),
        TranslationProvider.OpenAiCompatible => _openAiFactory(_configService.Config.GetActivePreset()),
        TranslationProvider.Ollama => _openAiFactory(new OpenAiPreset
        {
            ApiEndpoint = _configService.Config.Ollama.Endpoint + "/v1",
            ApiKey = "ollama",
            Model = _configService.Config.Ollama.Model,
            SystemPrompt = _configService.Config.GetActivePreset().SystemPrompt
        }),
        _ => _googleFactory()
    };
}
