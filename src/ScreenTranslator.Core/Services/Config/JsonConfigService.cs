using System.Text.Json;
using System.Text.Json.Serialization;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Core.Services.Config;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AppConfig))]
internal partial class AppConfigJsonContext : JsonSerializerContext;

public class JsonConfigService : IConfigService
{
    private readonly string _configPath;

    public AppConfig Config { get; private set; } = new();
    public event Action<AppConfig>? ConfigChanged;

    public JsonConfigService(string? configPath = null)
    {
        _configPath = configPath
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ScreenTranslator",
                "config.json");
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(_configPath))
        {
            await SaveAsync().ConfigureAwait(false);
            return;
        }

        var json = await File.ReadAllTextAsync(_configPath).ConfigureAwait(false);
        try
        {
            Config = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig) ?? new AppConfig();
        }
        catch (JsonException)
        {
            // Completely corrupted JSON — reset to defaults.
            Config = new AppConfig();
            await SaveAsync().ConfigureAwait(false);
        }
        ConfigChanged?.Invoke(Config);
    }

    public async Task SaveAsync()
    {
        var dir = Path.GetDirectoryName(_configPath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(Config, AppConfigJsonContext.Default.AppConfig);
        await File.WriteAllTextAsync(_configPath, json).ConfigureAwait(false);
        ConfigChanged?.Invoke(Config);
    }
}
