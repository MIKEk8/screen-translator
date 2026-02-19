using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Core.Services.Tts;

public class TtsRouter : ITtsService
{
    private readonly IConfigService _configService;
    private readonly SapiTtsService _sapiTts;
    private readonly OpenAiTtsService _openAiTts;
    private readonly DeepInfraTtsService _deepInfraTts;

    public bool IsSpeaking => GetCurrentProvider().IsSpeaking;

    public TtsRouter(IConfigService configService, SapiTtsService sapiTts, OpenAiTtsService openAiTts, DeepInfraTtsService deepInfraTts)
    {
        _configService = configService;
        _sapiTts = sapiTts;
        _openAiTts = openAiTts;
        _deepInfraTts = deepInfraTts;
    }

    public Task SpeakAsync(string text, CancellationToken ct = default) =>
        GetCurrentProvider().SpeakAsync(text, ct);

    public void Stop()
    {
        // Stop all providers — user may have switched while audio was playing
        _sapiTts.Stop();
        _openAiTts.Stop();
        _deepInfraTts.Stop();
    }

    public IReadOnlyList<string> GetAvailableVoices() =>
        GetCurrentProvider().GetAvailableVoices();

    private ITtsService GetCurrentProvider() =>
        _configService.Config.Tts.Provider switch
        {
            TtsProvider.OpenAiCompatible => _openAiTts,
            TtsProvider.DeepInfra => _deepInfraTts,
            _ => _sapiTts
        };
}
