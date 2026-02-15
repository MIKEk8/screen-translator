using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using ScreenTranslator.App.Windows;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;
using ScreenTranslator.Core.Services.Localization;
using ScreenTranslator.Core.Services.Translation;
using Serilog;

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
    private readonly ILocalizationService _loc;

    private bool _isLoading;

    private record ProviderOption(string Label, TranslationProvider Provider, string? PresetName = null)
    {
        public override string ToString() => Label;
    }

    /// <summary>
    /// OCR option: WindowsOcr, Tesseract, or a vision preset (PresetName set).
    /// </summary>
    private record OcrOption(string Label, OcrEngine Engine, string? PresetName = null)
    {
        public bool IsVision => PresetName is not null;
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
        _loc = App.Services.GetRequiredService<ILocalizationService>();

        var langCodes = SupportedLanguages.All.Select(l => l.Code.ToUpperInvariant()).ToList();
        SourceLangCombo.ItemsSource = langCodes;
        TargetLangCombo.ItemsSource = langCodes;

        Loaded += (_, _) =>
        {
            ApplyTranslations();
            SyncLanguageCombos();
            RebuildOcrCombo();
            RebuildProviderCombo();
        };
        _configService.ConfigChanged += _ => Dispatcher.Invoke(() =>
        {
            SyncLanguageCombos();
            RebuildOcrCombo();
            RebuildProviderCombo();
        });
        _loc.LanguageChanged += _ => Dispatcher.Invoke(() =>
        {
            ApplyTranslations();
            RebuildOcrCombo();
            RebuildProviderCombo();
        });
    }

    private void ApplyTranslations()
    {
        SourceLabel.Text = _loc.T("translate.source");
        TranslationLabel.Text = _loc.T("translate.translation");
        CaptureButton.Content = $"\U0001F4F7 {_loc.T("translate.capture")}";
        TranslateButton.Content = _loc.T("translate.translate");
        SpeakSourceButton.ToolTip = _loc.T("translate.speak_source");
        SpeakTargetButton.ToolTip = _loc.T("translate.speak_target");
        OcrCombo.ToolTip = _loc.T("translate.ocr_engine");
        ProviderCombo.ToolTip = _loc.T("translate.provider");
        if (StatusText.Text == "Ready" || StatusText.Text == _loc.T("status.ready"))
            StatusText.Text = _loc.T("status.ready");
    }

    private void RebuildOcrCombo()
    {
        _isLoading = true;

        var items = new List<OcrOption>
        {
            new(_loc.T("ocr.windows"), OcrEngine.WindowsOcr),
            new(_loc.T("ocr.tesseract"), OcrEngine.Tesseract),
        };
        foreach (var preset in _configService.Config.OpenAiPresets.Where(p => p.UseVision))
            items.Add(new(preset.Name, OcrEngine.Vision, preset.Name));

        OcrCombo.ItemsSource = items;

        var cfg = _configService.Config;
        if (cfg.OcrEngine == OcrEngine.Vision)
        {
            OcrCombo.SelectedItem = items.FirstOrDefault(i => i.PresetName == cfg.ActiveOcrPreset)
                                    ?? items.FirstOrDefault(i => i.IsVision);
        }
        else
        {
            OcrCombo.SelectedItem = items.FirstOrDefault(i => i.Engine == cfg.OcrEngine && !i.IsVision);
        }

        _isLoading = false;
    }

    private async void OcrCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || OcrCombo.SelectedItem is not OcrOption opt) return;
        _configService.Config.OcrEngine = opt.Engine;
        if (opt.PresetName is not null)
            _configService.Config.ActiveOcrPreset = opt.PresetName;
        await _configService.SaveAsync();
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
            var config = _configService.Config;

            if (config.OcrEngine == OcrEngine.Vision)
            {
                StatusText.Text = _loc.T("status.translating_vision");
                ShowSourceImage(screenshot.ImageData);

                // Use the vision preset selected in OCR combo
                var visionPreset = config.OpenAiPresets
                    .FirstOrDefault(p => p.Name == config.ActiveOcrPreset && p.UseVision)
                    ?? throw new InvalidOperationException("Vision preset not found");

                var visionService = new OpenAiTranslationService(visionPreset);
                var sw = Stopwatch.StartNew();
                var result = await visionService.TranslateImageAsync(
                    screenshot.ImageData,
                    config.SourceLanguage,
                    config.TargetLanguage);
                sw.Stop();

                TargetTextBox.Text = result.TranslatedText;

                var elapsed = sw.ElapsedMilliseconds < 1000
                    ? $"{sw.ElapsedMilliseconds}ms"
                    : $"{sw.Elapsed.TotalSeconds:F1}s";
                StatusText.Text = $"{result.Provider} — {elapsed}";

                if (config.Tts.AutoSpeakTranslation)
                    _ = SpeakText(result.TranslatedText);
            }
            else
            {
                ShowSourceText();
                StatusText.Text = _loc.T("status.recognizing");
                var ocrResult = await _ocrService.RecognizeAsync(
                    screenshot.ImageData,
                    config.SourceLanguage);

                var cleanText = ocrResult.Text.Replace("\r\n", " ").Replace("\n", " ");
                SourceTextBox.Text = cleanText;

                if (!string.IsNullOrWhiteSpace(cleanText))
                {
                    await TranslateText(cleanText);
                }
                else
                {
                    StatusText.Text = _loc.T("status.no_text");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OCR/translate failed");
            StatusText.Text = _loc.T("status.error", ex.Message);
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
            StatusText.Text = _loc.T("status.already_target", targetLang.ToUpperInvariant());
            if (_configService.Config.Tts.AutoSpeakTranslation)
                _ = SpeakText(text);
            return;
        }

        StatusText.Text = _loc.T("status.translating");
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
            Log.Error(ex, "Translation failed");
            StatusText.Text = _loc.T("status.translation_error", ex.Message);
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
                StatusText.Text = _loc.T("status.no_clipboard");
                return;
            }

            SourceTextBox.Text = text;
            StatusText.Text = _loc.T("status.translating");
            await TranslateText(text);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Copy & translate failed");
            StatusText.Text = _loc.T("status.error", ex.Message);
        }
    }

    public async void ProcessScreenshot(ScreenshotResult screenshot)
    {
        await OcrAndTranslate(screenshot);
    }

    private void ShowSourceImage(byte[] imageData)
    {
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = new MemoryStream(imageData);
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        SourceImage.Source = bitmapImage;
        SourceImage.Visibility = Visibility.Visible;
        SourceTextBox.Visibility = Visibility.Collapsed;
    }

    private void ShowSourceText()
    {
        SourceImage.Visibility = Visibility.Collapsed;
        SourceTextBox.Visibility = Visibility.Visible;
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
            StatusText.Text = _loc.T("status.speaking");
            await _ttsService.SpeakAsync(StripEmoji(text));
            StatusText.Text = _loc.T("status.ready");
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = _loc.T("status.ready");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "TTS failed");
            StatusText.Text = _loc.T("status.tts_error", ex.Message);
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
