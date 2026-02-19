using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Core.Services.Tts;

public partial class DeepInfraTtsService : ITtsService, IDisposable
{
    [GeneratedRegex(@"https?://\S+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    private const string BaseUrl = "https://api.deepinfra.com/v1/inference";

    private readonly IConfigService _configService;
    private readonly HttpClient _httpClient = new();
    private readonly Action<string> _playAudioFile;
    private readonly Action _stopAudio;
    private readonly Func<bool> _isAudioPlaying;
    private CancellationTokenSource? _speakCts;
    private string? _tempFile;

    public bool IsSpeaking => _isAudioPlaying();

    public DeepInfraTtsService(
        IConfigService configService,
        Action<string> playAudioFile,
        Action stopAudio,
        Func<bool> isAudioPlaying)
    {
        _configService = configService;
        _playAudioFile = playAudioFile;
        _stopAudio = stopAudio;
        _isAudioPlaying = isAudioPlaying;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task SpeakAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        text = UrlPattern().Replace(text, "").Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        Stop();

        _speakCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = _speakCts.Token;

        var cfg = _configService.Config.Tts;
        var model = cfg.DeepInfraModel;
        var url = $"{BaseUrl}/{model}";

        var requestBody = JsonSerializer.Serialize(new
        {
            text,
            voice = cfg.DeepInfraVoice,
            output_format = "mp3"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(cfg.DeepInfraApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.DeepInfraApiKey);

        var response = await _httpClient.SendAsync(request, token);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(token);
        var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("audio", out var audioProp))
            throw new InvalidOperationException("DeepInfra response missing 'audio' field");

        var audioValue = audioProp.GetString()
            ?? throw new InvalidOperationException("DeepInfra 'audio' field is null");

        // audio can be a base64 string or a data URI (data:audio/mp3;base64,...)
        byte[] audioBytes;
        if (audioValue.StartsWith("data:", StringComparison.Ordinal))
        {
            var commaIdx = audioValue.IndexOf(',');
            audioBytes = Convert.FromBase64String(audioValue[(commaIdx + 1)..]);
        }
        else
        {
            audioBytes = Convert.FromBase64String(audioValue);
        }

        token.ThrowIfCancellationRequested();

        var tempFile = Path.Combine(Path.GetTempPath(), "screen_translator_tts_di.mp3");
        await File.WriteAllBytesAsync(tempFile, audioBytes, token);

        _tempFile = tempFile;
        _playAudioFile(tempFile);
    }

    public void Stop()
    {
        _speakCts?.Cancel();
        _speakCts?.Dispose();
        _speakCts = null;
        _stopAudio();
    }

    public IReadOnlyList<string> GetAvailableVoices() =>
    [
        "af_heart", "af_bella", "af_nicole", "af_sarah", "af_sky",
        "am_adam", "am_michael",
        "bf_emma", "bf_isabella",
        "bm_george", "bm_lewis"
    ];

    public void Dispose()
    {
        Stop();
        _httpClient.Dispose();
        CleanupTempFile();
        GC.SuppressFinalize(this);
    }

    private void CleanupTempFile()
    {
        if (_tempFile is not null)
        {
            try { File.Delete(_tempFile); } catch { }
            _tempFile = null;
        }
    }
}
