using YamlDotNet.RepresentationModel;

namespace ScreenTranslator.Core.Services.Localization;

public record ValidationResult(
    bool IsValid,
    List<string> Missing,
    List<string> Outdated,
    List<string> Deprecated,
    string? YamlError);

public static class LocalizationValidator
{
    public static ValidationResult Validate(string yamlPath)
    {
        var missing = new List<string>();
        var outdated = new List<string>();
        var deprecated = new List<string>();

        // Try to parse YAML
        string text;
        try
        {
            text = File.ReadAllText(yamlPath);
        }
        catch (Exception ex)
        {
            return new ValidationResult(false, missing, outdated, deprecated,
                $"Cannot read file: {ex.Message}");
        }

        YamlMappingNode root;
        try
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(text));
            if (yaml.Documents.Count == 0)
                return new ValidationResult(false, missing, outdated, deprecated,
                    "YAML file is empty");
            root = (YamlMappingNode)yaml.Documents[0].RootNode;
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            var line = ex.Start.Line;
            return new ValidationResult(false, missing, outdated, deprecated,
                string.Format(TranslationKeys.All["validation.yaml_error"].Default, line, ex.Message));
        }

        // Read file version
        var fileVersion = GetScalarValue(root, "version") ?? "0.0.0";

        // Collect keys from YAML (excluding meta keys)
        var metaKeys = new HashSet<string> { "language", "language_name", "version" };
        var yamlKeys = new HashSet<string>();
        foreach (var kvp in root.Children)
        {
            var key = ((YamlScalarNode)kvp.Key).Value!;
            if (!metaKeys.Contains(key))
                yamlKeys.Add(key);
        }

        // Check each expected key
        foreach (var (key, entry) in TranslationKeys.All)
        {
            if (!yamlKeys.Contains(key))
            {
                missing.Add(key);
            }
            else if (CompareVersions(entry.Since, fileVersion) > 0)
            {
                outdated.Add($"{key} (changed in v{entry.Since})");
            }
        }

        // Check for deprecated keys
        foreach (var key in yamlKeys)
        {
            if (!TranslationKeys.All.ContainsKey(key))
                deprecated.Add(key);
        }

        var isValid = missing.Count == 0 && outdated.Count == 0 && deprecated.Count == 0;
        return new ValidationResult(isValid, missing, outdated, deprecated, null);
    }

    /// <summary>
    /// Compare two version strings. Returns &gt;0 if a &gt; b.
    /// </summary>
    private static int CompareVersions(string a, string b)
    {
        var va = ParseVersion(a);
        var vb = ParseVersion(b);
        return va.CompareTo(vb);
    }

    private static Version ParseVersion(string v)
    {
        return Version.TryParse(v, out var result) ? result : new Version(0, 0, 0);
    }

    private static string? GetScalarValue(YamlMappingNode root, string key)
    {
        foreach (var kvp in root.Children)
        {
            if (((YamlScalarNode)kvp.Key).Value == key)
                return (kvp.Value as YamlScalarNode)?.Value;
        }
        return null;
    }
}
