using System.IO;
using System.Windows;
using System.Windows.Media;

namespace ScreenTranslator.App;

public static class FeedbackSound
{
    private static MediaPlayer? _player;
    private static double _volume = 1.0;
    private static string? _tempFile;

    public static void SetVolume(int percent)
    {
        _volume = Math.Clamp(percent, 0, 100) / 100.0;
        if (_player is not null)
            _player.Volume = _volume;
    }

    public static void Play()
    {
        if (_volume <= 0) return;

        if (_player is null)
        {
            var sri = Application.GetResourceStream(new Uri("pack://application:,,,/feedback.wav", UriKind.Absolute));
            _tempFile = Path.Combine(Path.GetTempPath(), "ScreenTranslator_feedback.wav");
            using (var fs = File.Create(_tempFile))
                sri!.Stream.CopyTo(fs);

            _player = new MediaPlayer();
            _player.Volume = _volume;
            _player.Open(new Uri(_tempFile));
        }

        _player.Stop();
        _player.Position = TimeSpan.Zero;
        _player.Play();
    }
}
