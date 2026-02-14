using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Core.Services.Tts;

public partial class SapiTtsService : ITtsService, IDisposable
{
    [GeneratedRegex(@"https?://\S+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    private readonly SpeechSynthesizer _synth = new();
    private readonly IConfigService _configService;

    public bool IsSpeaking => _synth.State == SynthesizerState.Speaking;

    public SapiTtsService(IConfigService configService)
    {
        _configService = configService;
        ApplySettings();
        _configService.ConfigChanged += _ => ApplySettings();
    }

    private void ApplySettings()
    {
        // Stop speech before changing voice — SAPI5 crashes on SelectVoice mid-speech
        if (IsSpeaking)
            _synth.SpeakAsyncCancelAll();

        var cfg = _configService.Config.Tts;

        _synth.Rate = cfg.Rate;
        _synth.Volume = cfg.Volume;

        if (!string.IsNullOrEmpty(cfg.VoiceName))
        {
            try { _synth.SelectVoice(cfg.VoiceName); }
            catch { /* voice not installed, keep default */ }
        }
    }

    public Task SpeakAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return Task.CompletedTask;

        // Strip URLs before speaking
        text = UrlPattern().Replace(text, "").Trim();
        if (string.IsNullOrWhiteSpace(text)) return Task.CompletedTask;

        // Stop any ongoing speech before starting new
        if (IsSpeaking)
            _synth.SpeakAsyncCancelAll();

        ApplySettings();

        var tcs = new TaskCompletionSource();

        void OnCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            _synth.SpeakCompleted -= OnCompleted;
            if (e.Cancelled) tcs.TrySetCanceled();
            else if (e.Error != null) tcs.TrySetException(e.Error);
            else tcs.TrySetResult();
        }

        ct.Register(() =>
        {
            _synth.SpeakAsyncCancelAll();
        });

        _synth.SpeakCompleted += OnCompleted;
        _synth.SpeakAsync(text);

        return tcs.Task;
    }

    public void Stop()
    {
        if (IsSpeaking)
            _synth.SpeakAsyncCancelAll();
    }

    public IReadOnlyList<string> GetAvailableVoices()
    {
        return _synth.GetInstalledVoices()
            .Where(v => v.Enabled)
            .Select(v => v.VoiceInfo.Name)
            .ToList();
    }

    public void Dispose()
    {
        _synth.SpeakAsyncCancelAll();
        _synth.Dispose();
    }
}
