using ScreenTranslator.Core.Services.Localization;

namespace ScreenTranslator.Tests;

/// <summary>
/// Tests that the actual YAML translation files shipped with the app are complete and valid.
/// </summary>
public class YamlTranslationFileTests
{
    private static string TranslationsDir =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "ScreenTranslator.App", "translations");

    [Theory]
    [InlineData("en.yaml")]
    [InlineData("ru.yaml")]
    public void YamlFile_PassesValidation(string filename)
    {
        var path = Path.Combine(TranslationsDir, filename);
        if (!File.Exists(path))
        {
            // Skip if file not found (CI without source)
            return;
        }

        var result = LocalizationValidator.Validate(path);

        Assert.Null(result.YamlError);
        Assert.Empty(result.Missing);
        Assert.Empty(result.Deprecated);
    }

    [Theory]
    [InlineData("en.yaml")]
    [InlineData("ru.yaml")]
    public void YamlFile_HasCorrectVersion(string filename)
    {
        var path = Path.Combine(TranslationsDir, filename);
        if (!File.Exists(path))
            return;

        var result = LocalizationValidator.Validate(path);

        Assert.Empty(result.Outdated);
    }

    [Theory]
    [InlineData("en.yaml", "en", "English")]
    [InlineData("ru.yaml", "ru", "Русский")]
    public void YamlFile_HasCorrectMetadata(string filename, string expectedLang, string expectedName)
    {
        var path = Path.Combine(TranslationsDir, filename);
        if (!File.Exists(path))
            return;

        var svc = new LocalizationService(TranslationsDir);
        var langs = svc.GetAvailableLanguages();
        var lang = langs.FirstOrDefault(l => l.Code == expectedLang);

        Assert.NotNull(lang);
        Assert.Equal(expectedName, lang.Name);
    }

    [Fact]
    public void EnglishYaml_MatchesTranslationKeysDefaults()
    {
        var path = Path.Combine(TranslationsDir, "en.yaml");
        if (!File.Exists(path))
            return;

        var svc = new LocalizationService(TranslationsDir);
        svc.SetLanguage("en");

        // Every key from TranslationKeys should return the same value from YAML
        foreach (var (key, entry) in TranslationKeys.All)
        {
            var yamlValue = svc.T(key);
            Assert.Equal(entry.Default, yamlValue);
        }
    }

    [Fact]
    public void RussianYaml_HasNonEnglishValues()
    {
        var path = Path.Combine(TranslationsDir, "ru.yaml");
        if (!File.Exists(path))
            return;

        var svc = new LocalizationService(TranslationsDir);
        svc.SetLanguage("ru");

        // At least some Russian keys should differ from English defaults
        var diffCount = TranslationKeys.All
            .Count(kv => svc.T(kv.Key) != kv.Value.Default);

        Assert.True(diffCount > 20,
            $"Expected most Russian translations to differ from English, but only {diffCount} differ");
    }
}
