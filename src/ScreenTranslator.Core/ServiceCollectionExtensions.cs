using Microsoft.Extensions.DependencyInjection;
using ScreenTranslator.Core.Services.Config;
using ScreenTranslator.Core.Services.Hotkey;
using ScreenTranslator.Core.Services.Interfaces;
using ScreenTranslator.Core.Services.Localization;
using ScreenTranslator.Core.Services.Ocr;
using ScreenTranslator.Core.Services.Screenshot;
using ScreenTranslator.Core.Services.Translation;
using ScreenTranslator.Core.Services.Tts;

namespace ScreenTranslator.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScreenTranslatorCore(this IServiceCollection services, string translationsDir = "translations")
    {
        services.AddSingleton<ILocalizationService>(new LocalizationService(translationsDir));
        services.AddSingleton<IConfigService, JsonConfigService>();
        services.AddSingleton<IOcrService, OcrRouter>();
        services.AddSingleton<IScreenshotService, ScreenshotService>();
        services.AddSingleton<ITranslationService, TranslationRouter>();
        services.AddSingleton<SapiTtsService>();
        services.AddSingleton<MultiMonitorService>();
        services.AddSingleton<GlobalHotkeyService>();
        services.AddSingleton<IHotkeyService>(sp => sp.GetRequiredService<GlobalHotkeyService>());
        return services;
    }
}
