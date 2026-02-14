using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ScreenTranslator.App.Windows;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.App.Pages;

public partial class TranslatePage : Page
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const byte VK_MENU = 0x12;    // Alt
    private const byte VK_CONTROL = 0x11;
    private const byte VK_C = 0x43;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private readonly IOcrService _ocrService;
    private readonly ITranslationService _translationService;
    private readonly IScreenshotService _screenshotService;
    private readonly IConfigService _configService;
    private readonly ITtsService _ttsService;

    private ScreenRegion? _lastRegion;

    private bool _isLoading;

    private record ProviderOption(string Label, TranslationProvider Provider, string? PresetName = null)
    {
        public override string ToString() => Label;
    }

    public TranslatePage()
    {
        InitializeComponent();

        _ocrService = App.Services.GetRequiredService<IOcrService>();
        _translationService = App.Services.GetRequiredService<ITranslationService>();
        _screenshotService = App.Services.GetRequiredService<IScreenshotService>();
        _configService = App.Services.GetRequiredService<IConfigService>();
        _ttsService = App.Services.GetRequiredService<ITtsService>();

        var langCodes = SupportedLanguages.All.Select(l => l.Code.ToUpperInvariant()).ToList();
        SourceLangCombo.ItemsSource = langCodes;
        TargetLangCombo.ItemsSource = langCodes;

        Loaded += (_, _) =>
        {
            SyncLanguageCombos();
            RebuildProviderCombo();
        };
        _configService.ConfigChanged += _ => Dispatcher.Invoke(() =>
        {
            SyncLanguageCombos();
            RebuildProviderCombo();
        });
    }

    private void RebuildProviderCombo()
    {
        _isLoading = true;

        var items = new List<ProviderOption>
        {
            new("Google", TranslationProvider.Google),
        };
        foreach (var preset in _configService.Config.OpenAiPresets)
            items.Add(new(preset.Name, TranslationProvider.OpenAiCompatible, preset.Name));

        ProviderCombo.ItemsSource = items;

        // Select current
        var cfg = _configService.Config;
        if (cfg.TranslationProvider == TranslationProvider.OpenAiCompatible)
        {
            var active = cfg.ActiveOpenAiPreset;
            ProviderCombo.SelectedItem = items.FirstOrDefault(i => i.PresetName == active)
                                         ?? items.FirstOrDefault(i => i.Provider == TranslationProvider.OpenAiCompatible);
        }
        else
        {
            ProviderCombo.SelectedItem = items.FirstOrDefault(i => i.Provider == cfg.TranslationProvider);
        }

        _isLoading = false;
    }

    private async void ProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || ProviderCombo.SelectedItem is not ProviderOption opt) return;
        _configService.Config.TranslationProvider = opt.Provider;
        if (opt.PresetName is not null)
            _configService.Config.ActiveOpenAiPreset = opt.PresetName;
        await _configService.SaveAsync();
    }

    public async void StartAreaCapture()
    {
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow is null) return;

        var prevState = mainWindow.WindowState;
        mainWindow.WindowState = WindowState.Minimized;
        await Task.Delay(200);

        var selector = new AreaSelectorWindow();
        var result = selector.ShowDialog();

        if (result == true && selector.SelectedRegion is { } region)
        {
            _lastRegion = region;
            // Capture BEFORE restoring main window
            var screenshot = _screenshotService.CaptureRegion(region);
            mainWindow.WindowState = prevState;
            await OcrAndTranslate(screenshot);
        }
        else
        {
            mainWindow.WindowState = prevState;
        }
    }

    private async Task OcrAndTranslate(ScreenshotResult screenshot)
    {
        try
        {
            StatusText.Text = "Recognizing text...";
            var ocrResult = await _ocrService.RecognizeAsync(
                screenshot.ImageData,
                _configService.Config.SourceLanguage);

            var cleanText = ocrResult.Text.Replace("\r\n", " ").Replace("\n", " ");
            SourceTextBox.Text = cleanText;

            if (!string.IsNullOrWhiteSpace(cleanText))
            {
                await TranslateText(cleanText);
            }
            else
            {
                StatusText.Text = "No text detected";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private async Task TranslateText(string text)
    {
        var targetLang = _configService.Config.TargetLanguage;

        // Auto-detect: if text is already in the target language, skip translation
        var detected = DetectScript(text);
        if (detected is not null && detected == targetLang)
        {
            TargetTextBox.Text = text;
            StatusText.Text = $"Already in {targetLang.ToUpperInvariant()} — skipped";
            if (_configService.Config.Tts.AutoSpeakTranslation)
                _ = SpeakText(text);
            return;
        }

        StatusText.Text = "Translating...";
        try
        {
            var sw = Stopwatch.StartNew();
            var result = await _translationService.TranslateAsync(
                text,
                _configService.Config.SourceLanguage,
                targetLang);
            sw.Stop();

            TargetTextBox.Text = result.TranslatedText;

            var elapsed = sw.ElapsedMilliseconds < 1000
                ? $"{sw.ElapsedMilliseconds}ms"
                : $"{sw.Elapsed.TotalSeconds:F1}s";
            StatusText.Text = $"{result.Provider} — {elapsed}";

            if (_configService.Config.Tts.AutoSpeakTranslation)
                _ = SpeakText(result.TranslatedText);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Translation error: {ex.Message}";
        }
    }

    /// <summary>
    /// Detect the dominant script of text and map to a language code.
    /// Returns null if ambiguous or not enough signal.
    /// </summary>
    private static string? DetectScript(string text)
    {
        int cyrillic = 0, latin = 0, cjk = 0, arabic = 0, hangul = 0, kana = 0, total = 0;
        foreach (var ch in text)
        {
            if (!char.IsLetter(ch)) continue;
            total++;
            if (ch is >= '\u0400' and <= '\u04FF') cyrillic++;
            else if (ch is >= '\u0041' and <= '\u007A') latin++;
            else if (ch is >= '\u4E00' and <= '\u9FFF') cjk++;
            else if (ch is >= '\u0600' and <= '\u06FF') arabic++;
            else if (ch is >= '\uAC00' and <= '\uD7AF') hangul++;
            else if (ch is (>= '\u3040' and <= '\u309F') or (>= '\u30A0' and <= '\u30FF')) kana++;
        }

        if (total < 3) return null;

        var threshold = total * 0.6;
        if (cyrillic > threshold) return "ru";
        if (kana > threshold || (cjk > 0 && kana > 0 && cjk + kana > threshold)) return "ja";
        if (cjk > threshold) return "zh";
        if (hangul > threshold) return "ko";
        if (arabic > threshold) return "ar";
        // Latin is ambiguous (en/de/fr/es/etc) — only match "en" if target is "en"
        if (latin > threshold) return "en";

        return null;
    }

    private void SyncLanguageCombos()
    {
        _isLoading = true;
        SourceLangCombo.SelectedItem = _configService.Config.SourceLanguage.ToUpperInvariant();
        TargetLangCombo.SelectedItem = _configService.Config.TargetLanguage.ToUpperInvariant();
        _isLoading = false;
    }

    private async void SourceLangCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || SourceLangCombo.SelectedItem is not string code) return;
        _configService.Config.SourceLanguage = code.ToLowerInvariant();
        await _configService.SaveAsync();
    }

    private async void TargetLangCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || TargetLangCombo.SelectedItem is not string code) return;
        _configService.Config.TargetLanguage = code.ToLowerInvariant();
        await _configService.SaveAsync();
    }

    public async void CopyAndTranslate()
    {
        try
        {
            // Release Alt first — it's still held from the hotkey Alt+C
            keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            await Task.Delay(50);

            // Simulate Ctrl+C
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_C, 0, 0, UIntPtr.Zero);
            keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // Wait for clipboard to be populated
            await Task.Delay(200);

            var text = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text))
            {
                StatusText.Text = "No text in clipboard";
                return;
            }

            SourceTextBox.Text = text;
            StatusText.Text = "Translating...";
            await TranslateText(text);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    public bool HasLastRegion => _lastRegion is not null;

    public async void CaptureFromLastRegion()
    {
        if (_lastRegion is null)
        {
            StartAreaCapture();
            return;
        }

        var mainWindow = Application.Current.MainWindow;
        if (mainWindow is null) return;

        var prevState = mainWindow.WindowState;
        mainWindow.WindowState = WindowState.Minimized;
        await Task.Delay(200);

        var screenshot = _screenshotService.CaptureRegion(_lastRegion);
        mainWindow.WindowState = prevState;
        await OcrAndTranslate(screenshot);
    }

    private void CaptureButton_Click(object sender, RoutedEventArgs e) => StartAreaCapture();

    private async void TranslateButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(SourceTextBox.Text))
            await TranslateText(SourceTextBox.Text);
    }

    private async void SpeakSourceButton_Click(object sender, RoutedEventArgs e)
        => await SpeakText(SourceTextBox.Text);

    private async void SpeakTargetButton_Click(object sender, RoutedEventArgs e)
        => await SpeakText(TargetTextBox.Text);

    private async Task SpeakText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        if (_ttsService.IsSpeaking)
        {
            _ttsService.Stop();
            return;
        }

        try
        {
            StatusText.Text = "Speaking...";
            await _ttsService.SpeakAsync(StripEmoji(text));
            StatusText.Text = "Ready";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Ready";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"TTS error: {ex.Message}";
        }
    }

    /// <summary>
    /// Remove emoji and decorative Unicode symbols before TTS.
    /// Strips surrogate pairs (most modern emoji) and BMP symbol ranges.
    /// </summary>
    private static string StripEmoji(string text)
    {
        var sb = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            // Skip surrogate pairs (emoji in supplementary planes U+10000+)
            if (char.IsHighSurrogate(ch) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                i++;
                continue;
            }

            // Skip common BMP emoji/symbol ranges
            if (ch is (>= '\u2600' and <= '\u27BF')  // Misc Symbols, Dingbats
                   or '\u200D' or '\uFE0F' or '\u20E3' // ZWJ, variation selector, keycap
                   or (>= '\u2300' and <= '\u23FF')  // Misc Technical (⌚ etc)
                   or (>= '\u2B50' and <= '\u2B55')) // Stars, circles
                continue;

            sb.Append(ch);
        }
        return sb.ToString();
    }
}
