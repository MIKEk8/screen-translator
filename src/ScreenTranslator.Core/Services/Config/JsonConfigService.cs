using System.Text.Json;
using System.Text.Json.Serialization;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;
using ScreenTranslator.Core.Services.Security;

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
        DecryptApiKeys(Config);
        ConfigChanged?.Invoke(Config);
    }

    public async Task SaveAsync()
    {
        var dir = Path.GetDirectoryName(_configPath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        EncryptApiKeys(Config);
        var json = JsonSerializer.Serialize(Config, AppConfigJsonContext.Default.AppConfig);
        RestorePlaintextKeys(Config);
        await File.WriteAllTextAsync(_configPath, json).ConfigureAwait(false);
        ConfigChanged?.Invoke(Config);
    }

    /// <summary>
    /// Replace plaintext API keys with encrypted versions for serialization.
    /// Saves original values so they can be restored after writing.
    /// </summary>
    private static readonly Dictionary<string, string> _savedPlaintext = [];

    private static void EncryptApiKeys(AppConfig config)
    {
        _savedPlaintext.Clear();

        foreach (var preset in config.OpenAiPresets)
        {
            if (!string.IsNullOrEmpty(preset.ApiKey) && !SecureStorage.IsEncrypted(preset.ApiKey))
            {
                _savedPlaintext[$"preset:{preset.Name}"] = preset.ApiKey;
                preset.ApiKey = SecureStorage.Encrypt(preset.ApiKey);
            }
        }

        if (!string.IsNullOrEmpty(config.Tts.DeepInfraApiKey) && !SecureStorage.IsEncrypted(config.Tts.DeepInfraApiKey))
        {
            _savedPlaintext["deepinfra"] = config.Tts.DeepInfraApiKey;
            config.Tts.DeepInfraApiKey = SecureStorage.Encrypt(config.Tts.DeepInfraApiKey);
        }
    }

    /// <summary>
    /// Restore plaintext keys in memory after serialization.
    /// </summary>
    private static void RestorePlaintextKeys(AppConfig config)
    {
        foreach (var preset in config.OpenAiPresets)
        {
            if (_savedPlaintext.TryGetValue($"preset:{preset.Name}", out var plain))
                preset.ApiKey = plain;
        }

        if (_savedPlaintext.TryGetValue("deepinfra", out var diPlain))
            config.Tts.DeepInfraApiKey = diPlain;

        _savedPlaintext.Clear();
    }

    /// <summary>
    /// Decrypt API keys after deserialization.
    /// Handles graceful fallback if decryption fails (e.g. config from another user).
    /// </summary>
    private static void DecryptApiKeys(AppConfig config)
    {
        foreach (var preset in config.OpenAiPresets)
        {
            if (SecureStorage.IsEncrypted(preset.ApiKey))
            {
                try { preset.ApiKey = SecureStorage.Decrypt(preset.ApiKey); }
                catch { preset.ApiKey = ""; }
            }
        }

        if (SecureStorage.IsEncrypted(config.Tts.DeepInfraApiKey))
        {
            try { config.Tts.DeepInfraApiKey = SecureStorage.Decrypt(config.Tts.DeepInfraApiKey); }
            catch { config.Tts.DeepInfraApiKey = ""; }
        }
    }
}
