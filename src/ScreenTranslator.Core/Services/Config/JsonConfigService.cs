using System.Text.Json;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Core.Services.Config;

public class JsonConfigService : IConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
            Config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch (JsonException)
        {
            // Completely corrupted JSON — reset to defaults.
            // Note: invalid enum values (e.g. removed members) are handled gracefully
            // by TolerantEnumConverter which returns default instead of throwing.
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

        var json = JsonSerializer.Serialize(Config, JsonOptions);
        await File.WriteAllTextAsync(_configPath, json).ConfigureAwait(false);
        ConfigChanged?.Invoke(Config);
    }
}
