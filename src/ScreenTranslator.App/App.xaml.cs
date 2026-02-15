using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ScreenTranslator.App.ViewModels;
using ScreenTranslator.Core;
using ScreenTranslator.Core.Services.Localization;
using Serilog;

namespace ScreenTranslator.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Single instance check
        _mutex = new Mutex(true, "ScreenTranslator_SingleInstance", out bool isNew);
        if (!isNew)
        {
            // Localization not loaded yet, use hardcoded fallback
            MessageBox.Show("Screen Translator is already running.", "Screen Translator",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Setup Serilog file logging
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScreenTranslator", "logs", "log-.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Application starting, v{Version}", typeof(App).Assembly.GetName().Version);

        // Cleanup tray icon on unhandled WPF exceptions
        DispatcherUnhandledException += (_, args) =>
        {
            Log.Fatal(args.Exception, "Unhandled exception");
            (MainWindow as MainWindow)?.ForceCleanup();
            args.Handled = false;
        };

        try
        {
            var translationsDir = Path.Combine(AppContext.BaseDirectory, "translations");
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScreenTranslatorCore(translationsDir);
            serviceCollection.AddTransient<SettingsViewModel>();
            Services = serviceCollection.BuildServiceProvider();

            // Initialize localization after config is available
            var locService = Services.GetRequiredService<ILocalizationService>();
            var configService = Services.GetRequiredService<ScreenTranslator.Core.Services.Interfaces.IConfigService>();
            // Use Task.Run to avoid deadlock: LoadAsync uses async file I/O,
            // and .GetResult() on the WPF dispatcher thread would deadlock.
            Task.Run(() => configService.LoadAsync()).GetAwaiter().GetResult();
            var lang = configService.Config.InterfaceLanguage == "auto"
                ? locService.DetectSystemLanguage()
                : configService.Config.InterfaceLanguage;
            locService.SetLanguage(lang);

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Startup failed");
            MessageBox.Show($"Startup error: {ex}", "Screen Translator", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application exiting");
        Log.CloseAndFlush();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
