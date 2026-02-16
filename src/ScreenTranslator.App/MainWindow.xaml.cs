using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using ScreenTranslator.App.Pages;
using ScreenTranslator.App.Services;
using ScreenTranslator.App.Windows;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Hotkey;
using ScreenTranslator.Core.Services.Interfaces;
using ScreenTranslator.Core.Services.Localization;
using System.Runtime.InteropServices;
using Serilog;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace ScreenTranslator.App;

public partial class MainWindow : Window
{
    private readonly IConfigService _configService;
    private readonly IScreenshotService _screenshotService;
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly ITtsService _ttsService;
    private readonly ILocalizationService _loc;

    private TranslatePage? _translatePage;
    private PreviewPage? _previewPage;
    private SettingsPage? _settingsPage;
    private AboutPage? _aboutPage;
    private string _currentPage = "translate";

    private Forms.NotifyIcon? _trayIcon;
    private Drawing.Icon? _originalTrayIcon;
    private bool _forceClose;
    private bool _disposed;

    public static UpdateInfo? AvailableUpdate { get; internal set; }

    private GlobalMouseHookService? _mouseHookService;
    private GestureTrailWindow? _gestureTrailWindow;

    private const int WM_HOTKEY = 0x0312;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public MainWindow()
    {
        InitializeComponent();
        LoadIcon();

        _configService = App.Services.GetRequiredService<IConfigService>();
        _screenshotService = App.Services.GetRequiredService<IScreenshotService>();
        _hotkeyService = App.Services.GetRequiredService<GlobalHotkeyService>();
        _ttsService = App.Services.GetRequiredService<ITtsService>();
        _loc = App.Services.GetRequiredService<ILocalizationService>();

        InitializeTrayIcon();
        ApplyTranslations();
        _loc.LanguageChanged += _ => Dispatcher.Invoke(ApplyTranslations);

        // Cleanup tray icon on abnormal exit
        AppDomain.CurrentDomain.ProcessExit += (_, _) => CleanupTrayIcon();
        AppDomain.CurrentDomain.UnhandledException += (_, _) => CleanupTrayIcon();

        Loaded += MainWindow_Loaded;
    }

    private void LoadIcon()
    {
        try
        {
            var iconUri = new Uri("pack://application:,,,/icon.ico", UriKind.Absolute);
            var streamInfo = Application.GetResourceStream(iconUri);
            if (streamInfo != null)
            {
                // Window icon
                Icon = BitmapFrame.Create(streamInfo.Stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

                // Title bar small icon
                streamInfo = Application.GetResourceStream(iconUri);
                if (streamInfo != null)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = streamInfo.Stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    TitleBarIcon.Source = bitmap;
                }
            }
        }
        catch
        {
            // Graceful fallback — no icon
        }
    }

    private void ApplyTranslations()
    {
        Title = _loc.T("app.title");
        NavTranslate.ToolTip = _loc.T("nav.translate");
        NavPreview.ToolTip = _loc.T("nav.preview");
        NavSettings.ToolTip = _loc.T("nav.settings");
        NavAbout.ToolTip = _loc.T("nav.about");

        // Update tray menu
        if (_trayIcon?.ContextMenuStrip is { } menu)
        {
            menu.Items[0].Text = _loc.T("translate.capture");
            menu.Items[1].Text = _loc.T("tray.show");
            menu.Items[3].Text = _loc.T("tray.exit");
        }
    }

    private void InitializeTrayIcon()
    {
        var iconUri = new Uri("pack://application:,,,/icon.ico", UriKind.Absolute);
        var streamInfo = Application.GetResourceStream(iconUri);
        _originalTrayIcon = streamInfo != null
            ? new Drawing.Icon(streamInfo.Stream)
            : Drawing.SystemIcons.Application;

        _trayIcon = new Forms.NotifyIcon
        {
            Icon = _originalTrayIcon,
            Text = "Screen Translator",
            Visible = true
        };

        _trayIcon.DoubleClick += (_, _) => ShowFromTray();

        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.Add("Capture", null, (_, _) => Dispatcher.Invoke(HandleCaptureHotkey));
        contextMenu.Items.Add("Show", null, (_, _) => ShowFromTray());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, (_, _) =>
        {
            _forceClose = true;
            Close();
        });

        _trayIcon.ContextMenuStrip = contextMenu;
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _configService.LoadAsync();
        FeedbackSound.SetVolume(_configService.Config.NotificationVolume);
        _configService.ConfigChanged += cfg => Dispatcher.Invoke(() => FeedbackSound.SetVolume(cfg.NotificationVolume));

        // Navigate to translate page
        NavigateTo("translate");

        // Setup hotkey
        var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        hwndSource?.AddHook(WndProc);

        _hotkeyService.SetWindowHandle(new WindowInteropHelper(this).Handle);

        try
        {
            _hotkeyService.Register(
                _configService.Config.Hotkey.CaptureKey,
                () => Dispatcher.Invoke(HandleCaptureHotkey));
        }
        catch { }

        try
        {
            _hotkeyService.Register(
                _configService.Config.Hotkey.StopSpeechKey,
                () => _ttsService.Stop());
        }
        catch { }

        try
        {
            _hotkeyService.Register(
                _configService.Config.Hotkey.CopyTranslateKey,
                () => Dispatcher.Invoke(() =>
                {
                    _translatePage ??= new TranslatePage();
                    NavigateTo("translate");
                    _translatePage.CopyAndTranslate();
                }));
        }
        catch { }

        // Start background update checker
        UpdateChecker.Start(info => Dispatcher.Invoke(() => OnUpdateChecked(info)));

        // Setup mouse gesture hook and trail window
        _gestureTrailWindow = new GestureTrailWindow();
        _gestureTrailWindow.Show(); // Show once permanently (transparent + click-through)

        _mouseHookService = new GlobalMouseHookService(() => _configService.Config.Gesture);
        _mouseHookService.GestureStarted += OnGestureStarted;
        _mouseHookService.GesturePointAdded += OnGesturePointAdded;
        _mouseHookService.GestureCompleted += OnGestureCompleted;
        _mouseHookService.GestureCancelled += OnGestureCancelled;
        _mouseHookService.Install();
    }

    private void OnGestureStarted(double x, double y)
    {
        _gestureTrailWindow?.ShowTrail();
        _gestureTrailWindow?.AddPoint(x, y);
    }

    private void OnGesturePointAdded(double x, double y)
    {
        _gestureTrailWindow?.AddPoint(x, y);
    }

    private async void OnGestureCompleted(ScreenRegion region)
    {
        _gestureTrailWindow?.HideTrail();
        await Task.Delay(50); // let the trail window disappear before capture

        FeedbackSound.Play();

        Log.Information("Gesture completed: {Region}", region);

        _translatePage ??= new TranslatePage();
        NavigateTo("translate");

        var screenshot = _screenshotService.CaptureRegion(region);
        _translatePage.ProcessScreenshot(screenshot);
    }

    private void OnGestureCancelled()
    {
        _gestureTrailWindow?.HideTrail();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            handled = _hotkeyService.HandleMessage(wParam.ToInt32());
            if (handled)
            {
                FeedbackSound.Play();

                // Prevent Alt from activating the menu bar in the foreground app.
                // RegisterHotKey eats the key combo but not the WM_SYSKEYUP for Alt,
                // so the browser sees "bare Alt press" and opens the menu.
                // Injecting a dummy keypress breaks the "bare Alt" sequence.
                keybd_event(0, 0, 0, UIntPtr.Zero);
                keybd_event(0, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }
        return IntPtr.Zero;
    }

    private void OnUpdateChecked(UpdateInfo? info)
    {
        AvailableUpdate = info;
        if (info is not null)
        {
            // Badge on tray icon
            if (_originalTrayIcon is not null && _trayIcon is not null)
            {
                _trayIcon.Icon = CreateBadgedIcon(_originalTrayIcon);
                _trayIcon.Text = "Screen Translator — update available";
            }

            // Badge on About nav button
            UpdateBadge.Visibility = Visibility.Visible;
        }
    }

    private static Drawing.Icon CreateBadgedIcon(Drawing.Icon original)
    {
        using var bitmap = original.ToBitmap();
        using var g = Drawing.Graphics.FromImage(bitmap);
        var size = 10;
        var x = bitmap.Width - size - 1;
        var y = bitmap.Height - size - 1;
        using var brush = new Drawing.SolidBrush(Drawing.Color.FromArgb(74, 222, 128));
        g.FillEllipse(brush, x, y, size, size);
        return Drawing.Icon.FromHandle(bitmap.GetHicon());
    }

    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    private void HandleCaptureHotkey()
    {
        _translatePage ??= new TranslatePage();
        _translatePage.StartAreaCapture();
    }

    private void NavigateTo(string page)
    {
        _currentPage = page;

        // Update nav button styles
        NavTranslate.Style = (Style)FindResource(page == "translate" ? "NavButtonActive" : "NavButton");
        NavPreview.Style = (Style)FindResource(page == "preview" ? "NavButtonActive" : "NavButton");
        NavSettings.Style = (Style)FindResource(page == "settings" ? "NavButtonActive" : "NavButton");
        NavAbout.Style = (Style)FindResource(page == "about" ? "NavButtonActive" : "NavButton");

        switch (page)
        {
            case "translate":
                _translatePage ??= new TranslatePage();
                ContentFrame.Navigate(_translatePage);
                break;
            case "preview":
                _previewPage ??= new PreviewPage();
                ContentFrame.Navigate(_previewPage);
                break;
            case "settings":
                _settingsPage ??= new SettingsPage();
                ContentFrame.Navigate(_settingsPage);
                break;
            case "about":
                _aboutPage ??= new AboutPage();
                ContentFrame.Navigate(_aboutPage);
                break;
        }
    }

    private void NavTranslate_Click(object sender, RoutedEventArgs e) => NavigateTo("translate");
    private void NavPreview_Click(object sender, RoutedEventArgs e) => NavigateTo("preview");
    private void NavSettings_Click(object sender, RoutedEventArgs e) => NavigateTo("settings");
    private void NavAbout_Click(object sender, RoutedEventArgs e) => NavigateTo("about");

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        // Hide to tray instead of closing
        Hide();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_forceClose)
        {
            // Intercept close → hide to tray
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    public void ForceCleanup() => CleanupTrayIcon();

    private void CleanupTrayIcon()
    {
        if (_disposed) return;
        _disposed = true;

        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        CleanupTrayIcon();
        _mouseHookService?.Dispose();
        _gestureTrailWindow?.Close();
        _hotkeyService.Dispose();
        base.OnClosed(e);
    }
}
