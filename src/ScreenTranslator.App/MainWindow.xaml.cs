using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using ScreenTranslator.App.Pages;
using ScreenTranslator.Core.Services.Hotkey;
using ScreenTranslator.Core.Services.Interfaces;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace ScreenTranslator.App;

public partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_A = 0x41;
    private const int VK_MENU = 0x12; // Alt
    private const int LongPressMs = 2000;

    private readonly IConfigService _configService;
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly ITtsService _ttsService;

    private TranslatePage? _translatePage;
    private PreviewPage? _previewPage;
    private SettingsPage? _settingsPage;
    private AboutPage? _aboutPage;
    private string _currentPage = "translate";

    private Forms.NotifyIcon? _trayIcon;
    private bool _forceClose;
    private bool _disposed;

    private const int WM_HOTKEY = 0x0312;

    public MainWindow()
    {
        InitializeComponent();

        _configService = App.Services.GetRequiredService<IConfigService>();
        _hotkeyService = App.Services.GetRequiredService<GlobalHotkeyService>();
        _ttsService = App.Services.GetRequiredService<ITtsService>();

        InitializeTrayIcon();

        // Cleanup tray icon on abnormal exit
        AppDomain.CurrentDomain.ProcessExit += (_, _) => CleanupTrayIcon();
        AppDomain.CurrentDomain.UnhandledException += (_, _) => CleanupTrayIcon();

        Loaded += MainWindow_Loaded;
    }

    private void InitializeTrayIcon()
    {
        var iconUri = new Uri("pack://application:,,,/icon.ico", UriKind.Absolute);
        var streamInfo = Application.GetResourceStream(iconUri);
        var icon = streamInfo != null
            ? new Drawing.Icon(streamInfo.Stream)
            : Drawing.SystemIcons.Application;

        _trayIcon = new Forms.NotifyIcon
        {
            Icon = icon,
            Text = "Screen Translator",
            Visible = true
        };

        _trayIcon.DoubleClick += (_, _) => ShowFromTray();

        var contextMenu = new Forms.ContextMenuStrip();
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
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            handled = _hotkeyService.HandleMessage(wParam.ToInt32());
            if (handled)
                FeedbackSound.Play();
        }
        return IntPtr.Zero;
    }

    private async void HandleCaptureHotkey()
    {
        _translatePage ??= new TranslatePage();

        // If no previous region, go straight to area selector
        if (!_translatePage.HasLastRegion)
        {
            _translatePage.StartAreaCapture();
            return;
        }

        // Poll key state: if user holds Alt+A for 2 seconds → reuse last region
        var elapsed = 0;
        const int pollInterval = 50;

        while (elapsed < LongPressMs)
        {
            await Task.Delay(pollInterval);
            elapsed += pollInterval;

            // Check if both Alt and A are still held down (high bit set = key is down)
            bool altHeld = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
            bool aHeld = (GetAsyncKeyState(VK_A) & 0x8000) != 0;

            if (!altHeld || !aHeld)
            {
                // Released early → open area selector (short press)
                _translatePage.StartAreaCapture();
                return;
            }
        }

        // Held for 2 seconds → reuse previous region
        NavigateTo("translate");
        _translatePage.CaptureFromLastRegion();
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
        _hotkeyService.Dispose();
        base.OnClosed(e);
    }
}
