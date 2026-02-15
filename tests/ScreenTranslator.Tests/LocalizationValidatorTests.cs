using ScreenTranslator.Core.Services.Localization;

namespace ScreenTranslator.Tests;

public class LocalizationValidatorTests : IDisposable
{
    private readonly string _tempDir;

    public LocalizationValidatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"val_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string WriteYaml(string content)
    {
        var path = Path.Combine(_tempDir, $"test_{Guid.NewGuid()}.yaml");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Validate_CompleteFile_IsValid()
    {
        // Build a YAML with ALL keys from TranslationKeys
        var lines = new List<string>
        {
            "language: test",
            "language_name: Test",
            $"version: \"{TranslationKeys.CurrentVersion}\""
        };
        foreach (var key in TranslationKeys.All.Keys)
        {
            // Escape values for YAML
            var value = TranslationKeys.All[key].Default.Replace("\"", "\\\"");
            lines.Add($"{key}: \"{value}\"");
        }

        var path = WriteYaml(string.Join("\n", lines));
        var result = LocalizationValidator.Validate(path);

        Assert.True(result.IsValid, $"Expected valid, got missing={result.Missing.Count}, outdated={result.Outdated.Count}, deprecated={result.Deprecated.Count}");
        Assert.Empty(result.Missing);
        Assert.Empty(result.Outdated);
        Assert.Empty(result.Deprecated);
        Assert.Null(result.YamlError);
    }

    [Fact]
    public void Validate_MissingKeys_DetectsThem()
    {
        var path = WriteYaml("""
            language: test
            language_name: Test
            version: "1.1.0"
            app.title: Test
            """);

        var result = LocalizationValidator.Validate(path);

        Assert.False(result.IsValid);
        Assert.True(result.Missing.Count > 0);
        Assert.DoesNotContain("app.title", result.Missing);
        Assert.Contains("nav.translate", result.Missing);
    }

    [Fact]
    public void Validate_OutdatedKeys_DetectsThem()
    {
        // version 1.0.0 file but keys have Since="1.1.0"
        var lines = new List<string>
        {
            "language: test",
            "language_name: Test",
            "version: \"1.0.0\""
        };
        foreach (var key in TranslationKeys.All.Keys)
            lines.Add($"{key}: \"test value\"");

        var path = WriteYaml(string.Join("\n", lines));
        var result = LocalizationValidator.Validate(path);

        // Keys with Since="1.1.0" should be outdated (file version 1.0.0 < 1.1.0)
        var keysWithNewVersion = TranslationKeys.All
            .Where(kv => kv.Value.Since == "1.1.0")
            .Select(kv => kv.Key)
            .ToList();

        if (keysWithNewVersion.Count > 0)
        {
            Assert.True(result.Outdated.Count > 0, "Expected outdated keys for v1.0.0 file");
        }
    }

    [Fact]
    public void Validate_DeprecatedKeys_DetectsThem()
    {
        var lines = new List<string>
        {
            "language: test",
            "language_name: Test",
            $"version: \"{TranslationKeys.CurrentVersion}\""
        };
        foreach (var key in TranslationKeys.All.Keys)
            lines.Add($"{key}: \"value\"");
        // Add an extra key not in TranslationKeys
        lines.Add("old.removed.key: \"deprecated value\"");

        var path = WriteYaml(string.Join("\n", lines));
        var result = LocalizationValidator.Validate(path);

        Assert.Contains("old.removed.key", result.Deprecated);
    }

    [Fact]
    public void Validate_InvalidYaml_ReturnsYamlError()
    {
        var path = WriteYaml("not valid yaml: [[[{{{");

        var result = LocalizationValidator.Validate(path);

        Assert.False(result.IsValid);
        Assert.NotNull(result.YamlError);
    }

    [Fact]
    public void Validate_EmptyFile_ReturnsError()
    {
        var path = WriteYaml("");

        var result = LocalizationValidator.Validate(path);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NonExistentFile_ReturnsError()
    {
        var path = Path.Combine(_tempDir, "does_not_exist.yaml");

        var result = LocalizationValidator.Validate(path);

        Assert.False(result.IsValid);
        Assert.NotNull(result.YamlError);
    }

    [Fact]
    public void Validate_MetaKeysNotCountedAsMissing()
    {
        // Ensure "language", "language_name", "version" are not reported as deprecated
        var lines = new List<string>
        {
            "language: test",
            "language_name: Test",
            $"version: \"{TranslationKeys.CurrentVersion}\""
        };
        foreach (var key in TranslationKeys.All.Keys)
            lines.Add($"{key}: \"value\"");

        var path = WriteYaml(string.Join("\n", lines));
        var result = LocalizationValidator.Validate(path);

        Assert.DoesNotContain("language", result.Deprecated);
        Assert.DoesNotContain("language_name", result.Deprecated);
        Assert.DoesNotContain("version", result.Deprecated);
    }
}
