using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ScreenTranslator.Core.Services.Localization;

namespace ScreenTranslator.App.Pages;

public partial class AboutPage : Page
{
    private readonly ILocalizationService _loc;

    public AboutPage()
    {
        InitializeComponent();

        _loc = App.Services.GetRequiredService<ILocalizationService>();
        ApplyTranslations();
        _loc.LanguageChanged += _ => Dispatcher.Invoke(ApplyTranslations);

        Loaded += (_, _) => PopulateInfo();
    }

    private void ApplyTranslations()
    {
        AboutTitleText.Text = _loc.T("about.title");
        DescriptionText.Text = _loc.T("about.description");
        BuildTimeLbl.Text = _loc.T("about.build_time");
        RuntimeLbl.Text = _loc.T("about.runtime");
        PlatformLbl.Text = _loc.T("about.platform");
        TechSectionText.Text = _loc.T("about.technology");
        HotkeysSectionText.Text = _loc.T("about.hotkeys");
        HotkeyCaptureLbl.Text = _loc.T("about.hotkey_capture");
        HotkeyCopyLbl.Text = _loc.T("about.hotkey_copy");
        HotkeyStopLbl.Text = _loc.T("about.hotkey_stop");
        GestureSectionText.Text = _loc.T("about.gesture");
        GestureDescText.Text = _loc.T("about.gesture_desc");
        GestureConfigText.Text = _loc.T("about.gesture_config");
        FooterText.Text = _loc.T("about.footer");
    }

    private void PopulateInfo()
    {
        var asm = Assembly.GetExecutingAssembly();
        var version = asm.GetName().Version;

        VersionText.Text = $"v{version?.Major}.{version?.Minor}.{version?.Build}";

        // Build time = executable last write time
        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
        {
            var buildTime = File.GetLastWriteTime(exePath);
            BuildTimeText.Text = buildTime.ToString("yyyy-MM-dd HH:mm");
        }
        else
        {
            BuildTimeText.Text = "N/A";
        }

        RuntimeText.Text = $".NET {Environment.Version}";
        PlatformText.Text = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";
    }
}
