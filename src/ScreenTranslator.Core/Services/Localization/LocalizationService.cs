using System.Globalization;
using YamlDotNet.RepresentationModel;

namespace ScreenTranslator.Core.Services.Localization;

public interface ILocalizationService
{
    /// <summary>Current interface language code (e.g. "en", "ru").</summary>
    string CurrentLanguage { get; }

    /// <summary>Get translated string by key. Falls back to English default.</summary>
    string T(string key);

    /// <summary>Get translated string with format arguments.</summary>
    string T(string key, params object[] args);

    /// <summary>Change interface language. Reloads YAML file.</summary>
    void SetLanguage(string languageCode);

    /// <summary>List available translation files.</summary>
    IReadOnlyList<AvailableLanguage> GetAvailableLanguages();

    /// <summary>Detect best language from system culture.</summary>
    string DetectSystemLanguage();

    /// <summary>Fired when language changes.</summary>
    event Action<string>? LanguageChanged;
}

public record AvailableLanguage(string Code, string Name, string Version);

public class LocalizationService : ILocalizationService
{
    private readonly string _translationsDir;
    private Dictionary<string, string> _translations = new();

    public string CurrentLanguage { get; private set; } = "en";
    public event Action<string>? LanguageChanged;

    public LocalizationService(string translationsDir)
    {
        _translationsDir = translationsDir;
    }

    public void SetLanguage(string languageCode)
    {
        CurrentLanguage = languageCode;
        _translations = LoadYaml(languageCode);
        LanguageChanged?.Invoke(languageCode);
    }

    public string T(string key)
    {
        if (_translations.TryGetValue(key, out var value))
            return value;
        if (TranslationKeys.All.TryGetValue(key, out var entry))
            return entry.Default;
        return key;
    }

    public string T(string key, params object[] args)
    {
        var template = T(key);
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    public IReadOnlyList<AvailableLanguage> GetAvailableLanguages()
    {
        var result = new List<AvailableLanguage>();

        if (!Directory.Exists(_translationsDir))
            return result;

        foreach (var file in Directory.GetFiles(_translationsDir, "*.yaml"))
        {
            try
            {
                var (lang, name, version) = ReadYamlMeta(file);
                if (lang is not null)
                    result.Add(new AvailableLanguage(lang, name ?? lang, version ?? "?"));
            }
            catch { /* skip invalid files */ }
        }

        // Ensure English is always available (from built-in defaults)
        if (!result.Any(l => l.Code == "en"))
            result.Insert(0, new AvailableLanguage("en", "English", TranslationKeys.CurrentVersion));

        return result.OrderBy(l => l.Code).ToList();
    }

    public string DetectSystemLanguage()
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var available = GetAvailableLanguages();
        return available.Any(l => l.Code == culture) ? culture : "en";
    }

    private Dictionary<string, string> LoadYaml(string languageCode)
    {
        var path = Path.Combine(_translationsDir, $"{languageCode}.yaml");
        if (!File.Exists(path))
            return new Dictionary<string, string>();

        try
        {
            var text = File.ReadAllText(path);
            var yaml = new YamlStream();
            yaml.Load(new StringReader(text));

            if (yaml.Documents.Count == 0)
                return new Dictionary<string, string>();

            var root = (YamlMappingNode)yaml.Documents[0].RootNode;
            var dict = new Dictionary<string, string>();

            foreach (var kvp in root.Children)
            {
                var key = ((YamlScalarNode)kvp.Key).Value;
                if (key is "language" or "language_name" or "version")
                    continue;
                if (kvp.Value is YamlScalarNode scalar && scalar.Value is not null)
                    dict[key!] = scalar.Value;
            }

            return dict;
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private static (string? lang, string? name, string? version) ReadYamlMeta(string path)
    {
        var text = File.ReadAllText(path);
        var yaml = new YamlStream();
        yaml.Load(new StringReader(text));

        if (yaml.Documents.Count == 0)
            return (null, null, null);

        var root = (YamlMappingNode)yaml.Documents[0].RootNode;
        string? lang = null, name = null, version = null;

        foreach (var kvp in root.Children)
        {
            var key = ((YamlScalarNode)kvp.Key).Value;
            var val = (kvp.Value as YamlScalarNode)?.Value;
            switch (key)
            {
                case "language": lang = val; break;
                case "language_name": name = val; break;
                case "version": version = val; break;
            }
        }

        return (lang, name, version);
    }
}
