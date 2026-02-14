using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ScreenTranslator.App.ViewModels;
using ScreenTranslator.Core;

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
            MessageBox.Show("Screen Translator is already running.", "Screen Translator",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Cleanup tray icon on unhandled WPF exceptions
        DispatcherUnhandledException += (_, args) =>
        {
            (MainWindow as MainWindow)?.ForceCleanup();
            args.Handled = false;
        };

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScreenTranslatorCore();
        serviceCollection.AddTransient<SettingsViewModel>();
        Services = serviceCollection.BuildServiceProvider();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
