using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Core.Services.Tts;

public partial class OpenAiTtsService : ITtsService, IDisposable
{
    [GeneratedRegex(@"https?://\S+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    private readonly IConfigService _configService;
    private readonly HttpClient _httpClient = new();
    private readonly Action<string> _playAudioFile;
    private readonly Action _stopAudio;
    private readonly Func<bool> _isAudioPlaying;
    private CancellationTokenSource? _speakCts;
    private string? _tempFile;

    public bool IsSpeaking => _isAudioPlaying();

    public OpenAiTtsService(
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

        var cfg = _configService.Config;
        var preset = cfg.GetTtsPreset();
        var endpoint = preset.ApiEndpoint.TrimEnd('/');

        var requestBody = JsonSerializer.Serialize(new
        {
            model = cfg.Tts.TtsModel,
            input = text,
            voice = cfg.Tts.TtsVoice
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/audio/speech")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(preset.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", preset.ApiKey);

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();

        // Determine file extension from content type
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "audio/mpeg";
        var ext = contentType switch
        {
            "audio/wav" or "audio/wave" => ".wav",
            "audio/ogg" => ".ogg",
            "audio/flac" => ".flac",
            _ => ".mp3"
        };

        var tempFile = Path.Combine(Path.GetTempPath(), $"screen_translator_tts{ext}");
        await using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await response.Content.CopyToAsync(fileStream, token);
        }

        token.ThrowIfCancellationRequested();

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
        ["alloy", "echo", "fable", "onyx", "nova", "shimmer"];

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
