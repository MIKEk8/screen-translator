namespace ScreenTranslator.Core.Services.Interfaces;

public interface ITtsService
{
    Task SpeakAsync(string text, CancellationToken ct = default);
    void Stop();
    bool IsSpeaking { get; }
    IReadOnlyList<string> GetAvailableVoices();
}
