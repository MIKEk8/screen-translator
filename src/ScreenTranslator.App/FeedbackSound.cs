using System.Media;
using System.Windows;

namespace ScreenTranslator.App;

public static class FeedbackSound
{
    private static readonly SoundPlayer Player;

    static FeedbackSound()
    {
        var sri = Application.GetResourceStream(new Uri("pack://application:,,,/feedback.wav", UriKind.Absolute));
        Player = new SoundPlayer(sri!.Stream);
        Player.Load();
    }

    public static void Play()
    {
        Player.Play();
    }
}
