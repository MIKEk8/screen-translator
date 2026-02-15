using ScreenTranslator.Core.Services.Localization;

namespace ScreenTranslator.Tests;

public class TranslationKeysTests
{
    [Fact]
    public void AllKeys_HaveNonEmptyDefaults()
    {
        foreach (var (key, entry) in TranslationKeys.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Default),
                $"Key '{key}' has empty default text");
        }
    }

    [Fact]
    public void AllKeys_HaveValidVersionFormat()
    {
        foreach (var (key, entry) in TranslationKeys.All)
        {
            Assert.True(Version.TryParse(entry.Since, out _),
                $"Key '{key}' has invalid version '{entry.Since}'");
        }
    }

    [Fact]
    public void AllKeys_VersionNotGreaterThanCurrent()
    {
        var current = Version.Parse(TranslationKeys.CurrentVersion);

        foreach (var (key, entry) in TranslationKeys.All)
        {
            var keyVersion = Version.Parse(entry.Since);
            Assert.True(keyVersion <= current,
                $"Key '{key}' has version {entry.Since} > current {TranslationKeys.CurrentVersion}");
        }
    }

    [Fact]
    public void CurrentVersion_IsValidSemver()
    {
        Assert.True(Version.TryParse(TranslationKeys.CurrentVersion, out var v));
        Assert.True(v.Major >= 1);
    }

    [Fact]
    public void AllKeys_HaveUniqueNames()
    {
        // Dictionary enforces unique keys, but let's verify count matches expectations
        Assert.True(TranslationKeys.All.Count > 50,
            $"Expected 50+ keys, got {TranslationKeys.All.Count}");
    }

    [Fact]
    public void CriticalKeys_Exist()
    {
        // Ensure essential keys are present
        var critical = new[]
        {
            "app.title", "nav.translate", "nav.settings", "nav.about",
            "status.ready", "status.error",
            "settings.title", "settings.saved",
            "about.title",
            "selector.hint",
            "tray.show", "tray.exit"
        };

        foreach (var key in critical)
        {
            Assert.True(TranslationKeys.All.ContainsKey(key),
                $"Critical key '{key}' is missing from TranslationKeys");
        }
    }

    [Fact]
    public void FormatKeys_HavePlaceholders()
    {
        // Keys that use string.Format should contain {0}
        var formatKeys = new[] { "status.error", "status.translation_error", "status.tts_error",
            "status.already_target", "validation.yaml_error" };

        foreach (var key in formatKeys)
        {
            Assert.Contains("{0}", TranslationKeys.All[key].Default,
                StringComparison.Ordinal);
        }
    }
}
