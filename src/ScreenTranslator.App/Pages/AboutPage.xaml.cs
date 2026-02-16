using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
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

        Loaded += (_, _) =>
        {
            PopulateInfo();
            ShowCachedUpdate();
        };
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
        CheckUpdatesBtn.Content = _loc.T("about.check_updates");
        UpdateBtn.Content = _loc.T("about.update");
    }

    private void PopulateInfo()
    {
        var asm = Assembly.GetExecutingAssembly();
        var version = asm.GetName().Version;

        VersionText.Text = $"v{version?.Major}.{version?.Minor}.{version?.Build}";

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

    private void ShowCachedUpdate()
    {
        var info = MainWindow.AvailableUpdate;
        if (info is not null)
            ShowUpdateAvailable(info);
    }

    private void ShowUpdateAvailable(UpdateInfo info)
    {
        UpdateStatusText.Text = _loc.T("about.update_available", info.TagName);
        UpdateStatusText.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
        UpdateBtn.Visibility = Visibility.Visible;
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdatesBtn.IsEnabled = false;
        UpdateStatusText.Text = "...";
        UpdateStatusText.Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush");
        UpdateBtn.Visibility = Visibility.Collapsed;

        try
        {
            var info = await UpdateChecker.CheckNowAsync();
            if (info is not null)
            {
                MainWindow.AvailableUpdate = info;
                ShowUpdateAvailable(info);
            }
            else
            {
                UpdateStatusText.Text = _loc.T("about.up_to_date");
            }
        }
        catch
        {
            UpdateStatusText.Text = _loc.T("about.update_error");
        }
        finally
        {
            CheckUpdatesBtn.IsEnabled = true;
        }
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        var info = UpdateChecker.LatestUpdate;
        if (info is null) return;

        UpdateBtn.IsEnabled = false;
        CheckUpdatesBtn.IsEnabled = false;
        UpdateStatusText.Text = _loc.T("about.updating");

        try
        {
            var tempDir = Path.GetTempPath();
            var zipPath = Path.Combine(tempDir, "ScreenTranslator_update.zip");
            var stagingDir = Path.Combine(tempDir, "ScreenTranslator_staging");

            // Clean up previous attempts
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true);

            // Download zip
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "ScreenTranslator");
            var bytes = await http.GetByteArrayAsync(info.AssetUrl);
            await File.WriteAllBytesAsync(zipPath, bytes);

            // Extract to staging
            ZipFile.ExtractToDirectory(zipPath, stagingDir);
            File.Delete(zipPath);

            // Generate minimal bat script
            var appDir = AppContext.BaseDirectory.TrimEnd('\\');
            var scriptPath = Path.Combine(tempDir, "ScreenTranslator_update.bat");

            File.WriteAllText(scriptPath,
                "@echo off\r\n" +
                "timeout /t 2 /nobreak >nul\r\n" +
                $"xcopy \"{stagingDir}\\*\" \"{appDir}\\\" /s /y /q\r\n" +
                $"start \"\" \"{Path.Combine(appDir, "ScreenTranslator.App.exe")}\"\r\n" +
                $"rd /s /q \"{stagingDir}\"\r\n" +
                "del \"%~f0\"\r\n");

            // Launch script and close app
            Process.Start(new ProcessStartInfo(scriptPath)
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            if (Application.Current.MainWindow is MainWindow mainWindow)
                mainWindow.ForceClose();
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = _loc.T("status.error", ex.Message);
            UpdateBtn.IsEnabled = true;
            CheckUpdatesBtn.IsEnabled = true;
        }
    }
}
