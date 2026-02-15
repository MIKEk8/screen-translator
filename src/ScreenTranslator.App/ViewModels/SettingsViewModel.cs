using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;
using ScreenTranslator.Core.Services.Localization;

namespace ScreenTranslator.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    private readonly ITtsService _ttsService;
    private readonly ILocalizationService _locService;

    public IReadOnlyList<TranslationProvider> Providers { get; } = Enum.GetValues<TranslationProvider>();
    public IReadOnlyList<OcrEngine> OcrEngines { get; } = Enum.GetValues<OcrEngine>();
    public IReadOnlyList<string> AvailableVoices { get; }
    public IReadOnlyList<MouseButtonOption> MouseButtons { get; } =
    [
        new(1, "XButton1 (Back)"),
        new(2, "XButton2 (Forward)")
    ];

    [ObservableProperty]
    private TranslationProvider _selectedProvider;

    [ObservableProperty]
    private OcrEngine _selectedOcrEngine;

    [ObservableProperty]
    private string _captureHotkey = "Alt+A";

    [ObservableProperty]
    private string _copyTranslateHotkey = "Alt+C";

    [ObservableProperty]
    private string _stopSpeechHotkey = "Alt+X";

    [ObservableProperty]
    private double _overlayOpacity;

    [ObservableProperty]
    private int _overlayFontSize;

    [ObservableProperty]
    private bool _showOverlayOnTranslate;

    [ObservableProperty]
    private string _ollamaEndpoint = "http://localhost:11434";

    [ObservableProperty]
    private string _ollamaModel = "gemma3:4b";

    // OpenAI presets
    public ObservableCollection<OpenAiPreset> OpenAiPresets { get; } = [];

    [ObservableProperty]
    private OpenAiPreset? _selectedPreset;

    [ObservableProperty]
    private string _presetName = "Default";

    [ObservableProperty]
    private string _openAiEndpoint = "https://openrouter.ai/api/v1";

    [ObservableProperty]
    private string _openAiApiKey = "";

    [ObservableProperty]
    private string _openAiModel = "gpt-4o-mini";

    [ObservableProperty]
    private string _openAiSystemPrompt = "";

    [ObservableProperty]
    private bool _useVision;

    /// <summary>
    /// True when models aren't loaded yet (manual control) or selected model supports vision.
    /// </summary>
    public bool ShowVisionOption => !_modelsLoaded || _selectedModelSupportsVision;

    private bool _modelsLoaded;
    private bool _selectedModelSupportsVision;

    /// <summary>
    /// Update vision availability for the current model and refresh UI.
    /// </summary>
    public bool SelectedModelSupportsVision
    {
        get => _selectedModelSupportsVision;
        set
        {
            if (_selectedModelSupportsVision == value) return;
            _selectedModelSupportsVision = value;
            OnPropertyChanged(nameof(SelectedModelSupportsVision));
            OnPropertyChanged(nameof(ShowVisionOption));
        }
    }

    public ObservableCollection<ModelInfo> FilteredModels { get; } = [];
    private List<ModelInfo> _allModels = [];

    [ObservableProperty]
    private bool _isLoadingModels;

    [ObservableProperty]
    private string _selectedVoice = "";

    [ObservableProperty]
    private int _ttsRate = 8;

    [ObservableProperty]
    private int _ttsVolume = 100;

    [ObservableProperty]
    private bool _autoSpeakTranslation;

    [ObservableProperty]
    private bool _gestureEnabled;

    [ObservableProperty]
    private int _gestureMouseButton = 2;

    [ObservableProperty]
    private string _statusMessage = "";

    // ── Language ──
    public ObservableCollection<LanguageOption> AvailableInterfaceLanguages { get; } = [];

    [ObservableProperty]
    private LanguageOption? _selectedInterfaceLanguage;

    private bool _isLoading;
    private CancellationTokenSource? _autoSaveCts;

    public SettingsViewModel(IConfigService configService, ITtsService ttsService, ILocalizationService locService)
    {
        _configService = configService;
        _ttsService = ttsService;
        _locService = locService;
        AvailableVoices = ttsService.GetAvailableVoices();
        LoadLanguages();
        LoadFromConfig();
    }

    private void LoadLanguages()
    {
        AvailableInterfaceLanguages.Clear();
        AvailableInterfaceLanguages.Add(new LanguageOption("auto", "Auto (system)"));
        foreach (var lang in _locService.GetAvailableLanguages())
            AvailableInterfaceLanguages.Add(new LanguageOption(lang.Code, $"{lang.Name} ({lang.Code}) v{lang.Version}"));
    }

    private void LoadFromConfig()
    {
        _isLoading = true;
        var cfg = _configService.Config;

        SelectedProvider = cfg.TranslationProvider;
        SelectedOcrEngine = cfg.OcrEngine;
        CaptureHotkey = cfg.Hotkey.CaptureKey;
        CopyTranslateHotkey = cfg.Hotkey.CopyTranslateKey;
        StopSpeechHotkey = cfg.Hotkey.StopSpeechKey;
        OverlayOpacity = cfg.Overlay.Opacity;
        OverlayFontSize = cfg.Overlay.FontSize;
        ShowOverlayOnTranslate = cfg.Overlay.ShowOnTranslate;
        OllamaEndpoint = cfg.Ollama.Endpoint;
        OllamaModel = cfg.Ollama.Model;
        // Load presets
        OpenAiPresets.Clear();
        foreach (var p in cfg.OpenAiPresets)
            OpenAiPresets.Add(p);
        SelectedPreset = cfg.GetActivePreset();

        SelectedVoice = cfg.Tts.VoiceName;
        TtsRate = cfg.Tts.Rate;
        TtsVolume = cfg.Tts.Volume;
        AutoSpeakTranslation = cfg.Tts.AutoSpeakTranslation;

        GestureEnabled = cfg.Gesture.Enabled;
        GestureMouseButton = cfg.Gesture.MouseButton;

        // Language
        var langCode = cfg.InterfaceLanguage;
        SelectedInterfaceLanguage = AvailableInterfaceLanguages.FirstOrDefault(l => l.Code == langCode)
            ?? AvailableInterfaceLanguages.FirstOrDefault();

        _isLoading = false;
    }

    partial void OnSelectedPresetChanged(OpenAiPreset? value)
    {
        if (value is null) return;
        var wasLoading = _isLoading;
        _isLoading = true;
        PresetName = value.Name;
        OpenAiEndpoint = value.ApiEndpoint;
        OpenAiApiKey = value.ApiKey;
        OpenAiModel = value.Model;
        OpenAiSystemPrompt = value.SystemPrompt;
        UseVision = value.UseVision;
        SelectedModelSupportsVision = _allModels.FirstOrDefault(m => m.Id == value.Model)?.SupportsVision ?? value.UseVision;
        _isLoading = wasLoading;

        if (!_isLoading)
        {
            _configService.Config.ActiveOpenAiPreset = value.Name;
            ScheduleAutoSave();
        }
    }

    partial void OnPresetNameChanged(string value)
    {
        if (_isLoading || SelectedPreset is null || string.IsNullOrWhiteSpace(value)) return;
        SelectedPreset.Name = value;
        _configService.Config.ActiveOpenAiPreset = value;
        // Refresh ComboBox display
        var idx = OpenAiPresets.IndexOf(SelectedPreset);
        if (idx >= 0)
        {
            _isLoading = true;
            OpenAiPresets[idx] = SelectedPreset;
            SelectedPreset = OpenAiPresets[idx];
            _isLoading = false;
        }
        ScheduleAutoSave();
    }

    private void ApplyFieldsToSelectedPreset()
    {
        if (SelectedPreset is null) return;
        SelectedPreset.ApiEndpoint = OpenAiEndpoint;
        SelectedPreset.ApiKey = OpenAiApiKey;
        SelectedPreset.Model = OpenAiModel;
        SelectedPreset.SystemPrompt = OpenAiSystemPrompt;
        SelectedPreset.UseVision = UseVision;
    }

    [RelayCommand]
    private void AddPreset()
    {
        var name = $"Preset {OpenAiPresets.Count + 1}";
        // Copy endpoint/key from current preset for convenience
        var preset = new OpenAiPreset
        {
            Name = name,
            ApiEndpoint = SelectedPreset?.ApiEndpoint ?? "https://openrouter.ai/api/v1",
            ApiKey = SelectedPreset?.ApiKey ?? ""
        };
        OpenAiPresets.Add(preset);
        _configService.Config.OpenAiPresets = [.. OpenAiPresets];
        SelectedPreset = preset;
    }

    [RelayCommand]
    private void RemovePreset()
    {
        if (SelectedPreset is null || OpenAiPresets.Count <= 1) return;
        var idx = OpenAiPresets.IndexOf(SelectedPreset);
        OpenAiPresets.Remove(SelectedPreset);
        _configService.Config.OpenAiPresets = [.. OpenAiPresets];
        SelectedPreset = OpenAiPresets[Math.Min(idx, OpenAiPresets.Count - 1)];
    }

    [RelayCommand]
    private async Task FetchModelsAsync()
    {
        if (string.IsNullOrWhiteSpace(OpenAiEndpoint)) return;

        IsLoadingModels = true;
        StatusMessage = "Loading models...";
        try
        {
            using var http = new HttpClient();
            var endpoint = OpenAiEndpoint.TrimEnd('/');
            var request = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}/models");
            if (!string.IsNullOrWhiteSpace(OpenAiApiKey))
                request.Headers.Add("Authorization", $"Bearer {OpenAiApiKey}");

            var response = await http.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            _allModels.Clear();
            foreach (var model in doc.RootElement.GetProperty("data").EnumerateArray())
            {
                var id = model.GetProperty("id").GetString() ?? "";
                var name = model.TryGetProperty("name", out var n) ? n.GetString() ?? id : id;

                string? pricing = null;
                if (model.TryGetProperty("pricing", out var p))
                {
                    var prompt = p.TryGetProperty("prompt", out var pp) ? pp.GetString() : null;
                    var completion = p.TryGetProperty("completion", out var cp) ? cp.GetString() : null;
                    if (prompt is not null && completion is not null)
                    {
                        if (double.TryParse(prompt, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var pv) &&
                            double.TryParse(completion, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var cv))
                        {
                            pricing = $"${pv * 1_000_000:F2} / ${cv * 1_000_000:F2} per 1M tok";
                        }
                    }
                }

                var supportsVision = false;
                if (model.TryGetProperty("architecture", out var arch) &&
                    arch.TryGetProperty("input_modalities", out var modalities))
                {
                    supportsVision = modalities.EnumerateArray()
                        .Any(m => m.GetString() is "image");
                }

                _allModels.Add(new ModelInfo(id, name, pricing, supportsVision));
            }

            FilterModels("");
            _modelsLoaded = true;
            SelectedModelSupportsVision = _allModels.FirstOrDefault(m => m.Id == OpenAiModel)?.SupportsVision ?? false;
            StatusMessage = $"Loaded {_allModels.Count} models";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoadingModels = false;
        }
    }

    public bool HasModels => _allModels.Count > 0;

    public void FilterModels(string query)
    {
        FilteredModels.Clear();
        var q = query?.Trim() ?? "";
        var source = string.IsNullOrEmpty(q) ? _allModels : _allModels.Where(m =>
            m.Id.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            m.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
        foreach (var m in source.Take(50))
            FilteredModels.Add(m);
    }

    private void ScheduleAutoSave()
    {
        if (_isLoading) return;
        _autoSaveCts?.Cancel();
        _autoSaveCts = new CancellationTokenSource();
        _ = AutoSaveAfterDelayAsync(_autoSaveCts.Token);
    }

    private async Task AutoSaveAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(500, token);
            await PersistAsync();
            StatusMessage = "Saved";
        }
        catch (TaskCanceledException) { }
    }

    partial void OnSelectedProviderChanged(TranslationProvider value) => ScheduleAutoSave();
    partial void OnSelectedOcrEngineChanged(OcrEngine value) => ScheduleAutoSave();
    partial void OnCaptureHotkeyChanged(string value) => ScheduleAutoSave();
    partial void OnCopyTranslateHotkeyChanged(string value) => ScheduleAutoSave();
    partial void OnStopSpeechHotkeyChanged(string value) => ScheduleAutoSave();
    partial void OnOverlayOpacityChanged(double value) => ScheduleAutoSave();
    partial void OnOverlayFontSizeChanged(int value) => ScheduleAutoSave();
    partial void OnShowOverlayOnTranslateChanged(bool value) => ScheduleAutoSave();
    partial void OnOllamaEndpointChanged(string value) => ScheduleAutoSave();
    partial void OnOllamaModelChanged(string value) => ScheduleAutoSave();
    partial void OnOpenAiEndpointChanged(string value) { if (!_isLoading) ApplyFieldsToSelectedPreset(); ScheduleAutoSave(); }
    partial void OnOpenAiApiKeyChanged(string value) { if (!_isLoading) ApplyFieldsToSelectedPreset(); ScheduleAutoSave(); }
    partial void OnOpenAiModelChanged(string value) { if (!_isLoading) ApplyFieldsToSelectedPreset(); ScheduleAutoSave(); }
    partial void OnOpenAiSystemPromptChanged(string value) { if (!_isLoading) ApplyFieldsToSelectedPreset(); ScheduleAutoSave(); }
    partial void OnUseVisionChanged(bool value) { if (!_isLoading) ApplyFieldsToSelectedPreset(); ScheduleAutoSave(); }
    partial void OnSelectedVoiceChanged(string value) => ScheduleAutoSave();
    partial void OnTtsRateChanged(int value) => ScheduleAutoSave();
    partial void OnTtsVolumeChanged(int value) => ScheduleAutoSave();
    partial void OnAutoSpeakTranslationChanged(bool value) => ScheduleAutoSave();
    partial void OnGestureEnabledChanged(bool value) => ScheduleAutoSave();
    partial void OnGestureMouseButtonChanged(int value) => ScheduleAutoSave();

    partial void OnSelectedInterfaceLanguageChanged(LanguageOption? value)
    {
        if (_isLoading || value is null) return;
        _configService.Config.InterfaceLanguage = value.Code;
        var lang = value.Code == "auto"
            ? _locService.DetectSystemLanguage()
            : value.Code;
        _locService.SetLanguage(lang);
        ScheduleAutoSave();
    }

    [RelayCommand]
    private void ValidateTranslations()
    {
        var yamlPath = Path.Combine(AppContext.BaseDirectory, "translations", $"{_locService.CurrentLanguage}.yaml");
        if (!File.Exists(yamlPath))
        {
            MessageBox.Show(
                $"No translation file found for '{_locService.CurrentLanguage}'.\nUsing built-in English defaults.",
                _locService.T("validation.title"),
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = LocalizationValidator.Validate(yamlPath);

        if (result.YamlError is not null)
        {
            MessageBox.Show(result.YamlError, _locService.T("validation.title"),
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (result.IsValid)
        {
            MessageBox.Show(_locService.T("validation.ok"), _locService.T("validation.title"),
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sb = new System.Text.StringBuilder();
        if (result.Missing.Count > 0)
        {
            sb.AppendLine(_locService.T("validation.missing"));
            foreach (var k in result.Missing)
                sb.AppendLine($"  - {k}");
            sb.AppendLine();
        }
        if (result.Outdated.Count > 0)
        {
            sb.AppendLine(_locService.T("validation.outdated"));
            foreach (var k in result.Outdated)
                sb.AppendLine($"  - {k}");
            sb.AppendLine();
        }
        if (result.Deprecated.Count > 0)
        {
            sb.AppendLine(_locService.T("validation.deprecated"));
            foreach (var k in result.Deprecated)
                sb.AppendLine($"  - {k}");
        }

        MessageBox.Show(sb.ToString().TrimEnd(), _locService.T("validation.title"),
            MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private async Task PersistAsync()
    {
        var cfg = _configService.Config;

        cfg.TranslationProvider = SelectedProvider;
        cfg.OcrEngine = SelectedOcrEngine;
        cfg.Hotkey.CaptureKey = CaptureHotkey;
        cfg.Hotkey.CopyTranslateKey = CopyTranslateHotkey;
        cfg.Hotkey.StopSpeechKey = StopSpeechHotkey;
        cfg.Overlay.Opacity = OverlayOpacity;
        cfg.Overlay.FontSize = OverlayFontSize;
        cfg.Overlay.ShowOnTranslate = ShowOverlayOnTranslate;
        cfg.Ollama.Endpoint = OllamaEndpoint;
        cfg.Ollama.Model = OllamaModel;
        cfg.OpenAiPresets = [.. OpenAiPresets];
        cfg.Tts.VoiceName = SelectedVoice;
        cfg.Tts.Rate = TtsRate;
        cfg.Tts.Volume = TtsVolume;
        cfg.Tts.AutoSpeakTranslation = AutoSpeakTranslation;

        cfg.Gesture.Enabled = GestureEnabled;
        cfg.Gesture.MouseButton = GestureMouseButton;
        cfg.InterfaceLanguage = SelectedInterfaceLanguage?.Code ?? "auto";

        await _configService.SaveAsync();
    }

    [RelayCommand]
    private async Task TestVoiceAsync()
    {
        if (_ttsService.IsSpeaking)
        {
            _ttsService.Stop();
            return;
        }

        var cfg = _configService.Config;
        var prevVoice = cfg.Tts.VoiceName;
        var prevRate = cfg.Tts.Rate;
        var prevVolume = cfg.Tts.Volume;

        cfg.Tts.VoiceName = SelectedVoice;
        cfg.Tts.Rate = TtsRate;
        cfg.Tts.Volume = TtsVolume;

        try
        {
            await _ttsService.SpeakAsync("Привет! Это тестовое сообщение для проверки голоса.");
        }
        catch { /* ignore */ }
        finally
        {
            cfg.Tts.VoiceName = prevVoice;
            cfg.Tts.Rate = prevRate;
            cfg.Tts.Volume = prevVolume;
        }
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        var fresh = new AppConfig();
        _configService.Config.SourceLanguage = fresh.SourceLanguage;
        _configService.Config.TargetLanguage = fresh.TargetLanguage;
        _configService.Config.TranslationProvider = fresh.TranslationProvider;
        _configService.Config.OcrEngine = fresh.OcrEngine;
        _configService.Config.Hotkey = fresh.Hotkey;
        _configService.Config.Overlay = fresh.Overlay;
        _configService.Config.Ollama = fresh.Ollama;
        _configService.Config.OpenAiPresets = fresh.OpenAiPresets;
        _configService.Config.ActiveOpenAiPreset = fresh.ActiveOpenAiPreset;
        _configService.Config.Tts = fresh.Tts;
        _configService.Config.AutoDetectLanguage = fresh.AutoDetectLanguage;
        _configService.Config.Gesture = fresh.Gesture;
        LoadFromConfig();
        await _configService.SaveAsync();
        StatusMessage = "Reset to defaults";
    }
}

public record MouseButtonOption(int Value, string Label)
{
    public override string ToString() => Label;
}

public record ModelInfo(string Id, string Name, string? Pricing, bool SupportsVision)
{
    public override string ToString()
    {
        var vision = SupportsVision ? " [vision]" : "";
        return Pricing is not null ? $"{Name}{vision}  ({Pricing})" : $"{Name}{vision}";
    }
}

public record LanguageOption(string Code, string Label)
{
    public override string ToString() => Label;
}
