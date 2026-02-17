using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ScreenTranslator.App.ViewModels;
using ScreenTranslator.Core.Services.Localization;

namespace ScreenTranslator.App.Pages;

public partial class SettingsPage : Page
{
    private readonly ILocalizationService _loc;
    private bool _suppressModelPopup;

    public SettingsPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SettingsViewModel>();

        _loc = App.Services.GetRequiredService<ILocalizationService>();
        ApplyTranslations();
        _loc.LanguageChanged += _ => Dispatcher.Invoke(ApplyTranslations);
    }

    private void ApplyTranslations()
    {
        TitleText.Text = _loc.T("settings.title");

        AppSectionText.Text = _loc.T("settings.application");
        AutostartCheckBox.Content = _loc.T("settings.autostart");
        StartMinimizedCheckBox.Content = _loc.T("settings.start_minimized");

        LangSectionText.Text = _loc.T("settings.language");
        InterfaceLangText.Text = _loc.T("settings.interface_lang");
        ValidateButton.Content = _loc.T("settings.validate");

        TranslationSectionText.Text = _loc.T("settings.translation");
        ProviderText.Text = _loc.T("settings.provider");
        OcrEngineText.Text = _loc.T("settings.ocr_engine");

        OpenAiTitleText.Text = _loc.T("settings.openai_title");
        PresetText.Text = _loc.T("settings.preset");
        AddPresetBtn.ToolTip = _loc.T("settings.add_preset");
        RemovePresetBtn.ToolTip = _loc.T("settings.remove_preset");
        NameText.Text = _loc.T("settings.name");
        EndpointText.Text = _loc.T("settings.endpoint");
        ApiKeyText.Text = _loc.T("settings.api_key");
        ModelText.Text = _loc.T("settings.model");
        FetchModelsBtn.ToolTip = _loc.T("settings.fetch_models");
        SystemPromptText.Text = _loc.T("settings.system_prompt");
        VisionCheckBox.Content = _loc.T("settings.vision_mode");

        OllamaTitleText.Text = _loc.T("settings.ollama_title");
        OllamaEndpointText.Text = _loc.T("settings.endpoint");
        OllamaModelText.Text = _loc.T("settings.model");

        TtsSectionText.Text = _loc.T("settings.tts");
        TtsProviderText.Text = _loc.T("settings.tts_provider");
        VoiceText.Text = _loc.T("settings.voice");
        SpeedText.Text = _loc.T("settings.speed");
        VolumeText.Text = _loc.T("settings.volume");
        TtsOpenAiTitle.Text = _loc.T("settings.tts_openai_title");
        TtsPresetText.Text = _loc.T("settings.tts_preset");
        TtsModelText.Text = _loc.T("settings.tts_model");
        TtsVoiceText.Text = _loc.T("settings.tts_voice");
        AutoSpeakCheckBox.Content = _loc.T("settings.auto_speak");
        TestVoiceBtn.Content = $"\u25B6 {_loc.T("settings.test")}";

        NotificationVolumeText.Text = _loc.T("settings.notification_volume");

        HotkeysSectionText.Text = _loc.T("settings.hotkeys");
        HotkeyCaptureText.Text = _loc.T("settings.hotkey_capture");
        HotkeyCopyText.Text = _loc.T("settings.hotkey_copy");
        HotkeyStopText.Text = _loc.T("settings.hotkey_stop");

        GestureSectionText.Text = _loc.T("settings.gesture");
        GestureEnableCheckBox.Content = _loc.T("settings.gesture_enable");
        GestureButtonText.Text = _loc.T("settings.gesture_button");
        GestureHintText.Text = _loc.T("settings.gesture_hint");

        OverlaySectionText.Text = _loc.T("settings.overlay");
        OverlayShowCheckBox.Content = _loc.T("settings.overlay_show");
        OpacityText.Text = _loc.T("settings.opacity");
        FontSizeText.Text = _loc.T("settings.font_size");

        ResetButton.Content = _loc.T("settings.reset");
    }

    private void ModelSearchBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (_suppressModelPopup) return;
        if (DataContext is SettingsViewModel vm && vm.HasModels)
        {
            vm.FilterModels(vm.OpenAiModel);
            ModelPopup.IsOpen = vm.FilteredModels.Count > 0;
        }
    }

    private void ModelSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressModelPopup) return;
        if (DataContext is SettingsViewModel vm && vm.HasModels)
        {
            vm.FilterModels(vm.OpenAiModel);
            ModelPopup.IsOpen = vm.FilteredModels.Count > 0;
        }
    }

    private void ModelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: ModelInfo model } && DataContext is SettingsViewModel vm)
        {
            _suppressModelPopup = true;
            vm.OpenAiModel = model.Id;
            vm.SelectedModelSupportsVision = model.SupportsVision;
            if (!model.SupportsVision)
                vm.UseVision = false;
            ModelPopup.IsOpen = false;
            _suppressModelPopup = false;
        }
    }

    private async void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            _loc.T("settings.reset_confirm"),
            _loc.T("settings.reset"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes && DataContext is SettingsViewModel vm)
            await vm.ResetCommand.ExecuteAsync(null);
    }
}
