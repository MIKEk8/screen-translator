using ScreenTranslator.Core.Services.Localization;

namespace ScreenTranslator.Tests;

public class LocalizationServiceTests : IDisposable
{
    private readonly string _tempDir;

    public LocalizationServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"loc_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private void WriteYaml(string filename, string content)
    {
        File.WriteAllText(Path.Combine(_tempDir, filename), content);
    }

    [Fact]
    public void T_WithNoYaml_ReturnsFallbackFromTranslationKeys()
    {
        var svc = new LocalizationService(_tempDir);
        svc.SetLanguage("en");

        var result = svc.T("app.title");

        Assert.Equal("Screen Translator", result);
    }

    [Fact]
    public void T_WithYaml_ReturnsTranslatedValue()
    {
        WriteYaml("de.yaml", """
            language: de
            language_name: Deutsch
            version: "1.1.0"
            app.title: Bildschirmübersetzer
            """);

        var svc = new LocalizationService(_tempDir);
        svc.SetLanguage("de");

        Assert.Equal("Bildschirmübersetzer", svc.T("app.title"));
    }

    [Fact]
    public void T_MissingKeyInYaml_FallsBackToDefault()
    {
        WriteYaml("fr.yaml", """
            language: fr
            language_name: Français
            version: "1.1.0"
            app.title: Traducteur d'écran
            """);

        var svc = new LocalizationService(_tempDir);
        svc.SetLanguage("fr");

        // Key exists in YAML
        Assert.Equal("Traducteur d'écran", svc.T("app.title"));
        // Key missing from YAML — should fallback to English default
        Assert.Equal("Settings", svc.T("settings.title"));
    }

    [Fact]
    public void T_UnknownKey_ReturnsKeyItself()
    {
        var svc = new LocalizationService(_tempDir);
        svc.SetLanguage("en");

        Assert.Equal("nonexistent.key", svc.T("nonexistent.key"));
    }

    [Fact]
    public void T_WithFormatArgs_FormatsCorrectly()
    {
        var svc = new LocalizationService(_tempDir);
        svc.SetLanguage("en");

        var result = svc.T("status.error", "connection timeout");

        Assert.Equal("Error: connection timeout", result);
    }

    [Fact]
    public void T_WithFormatArgs_InvalidFormat_ReturnsTemplate()
    {
        WriteYaml("bad.yaml", """
            language: bad
            language_name: Bad
            version: "1.0.0"
            status.error: "Error without placeholder"
            """);

        var svc = new LocalizationService(_tempDir);
        svc.SetLanguage("bad");

        // No {0} placeholder, so format should not crash
        var result = svc.T("status.error", "something");
        Assert.Equal("Error without placeholder", result);
    }

    [Fact]
    public void SetLanguage_ChangesCurrentLanguage()
    {
        var svc = new LocalizationService(_tempDir);

        svc.SetLanguage("ru");
        Assert.Equal("ru", svc.CurrentLanguage);

        svc.SetLanguage("en");
        Assert.Equal("en", svc.CurrentLanguage);
    }

    [Fact]
    public void SetLanguage_FiresLanguageChangedEvent()
    {
        var svc = new LocalizationService(_tempDir);
        string? firedLang = null;
        svc.LanguageChanged += lang => firedLang = lang;

        svc.SetLanguage("ja");

        Assert.Equal("ja", firedLang);
    }

    [Fact]
    public void GetAvailableLanguages_ReturnsYamlFiles()
    {
        WriteYaml("en.yaml", """
            language: en
            language_name: English
            version: "1.1.0"
            """);
        WriteYaml("ru.yaml", """
            language: ru
            language_name: Русский
            version: "1.1.0"
            """);

        var svc = new LocalizationService(_tempDir);
        var langs = svc.GetAvailableLanguages();

        Assert.True(langs.Count >= 2);
        Assert.Contains(langs, l => l.Code == "en" && l.Name == "English");
        Assert.Contains(langs, l => l.Code == "ru" && l.Name == "Русский");
    }

    [Fact]
    public void GetAvailableLanguages_AlwaysIncludesEnglish()
    {
        // No YAML files at all
        var svc = new LocalizationService(_tempDir);
        var langs = svc.GetAvailableLanguages();

        Assert.Contains(langs, l => l.Code == "en");
    }

    [Fact]
    public void GetAvailableLanguages_EmptyDir_DoesNotCrash()
    {
        var nonExistent = Path.Combine(_tempDir, "no_such_dir");
        var svc = new LocalizationService(nonExistent);
        var langs = svc.GetAvailableLanguages();

        Assert.NotNull(langs);
    }

    [Fact]
    public void SetLanguage_MissingYamlFile_UsesDefaults()
    {
        var svc = new LocalizationService(_tempDir);
        svc.SetLanguage("xx"); // No xx.yaml

        // Should still return English default
        Assert.Equal("Screen Translator", svc.T("app.title"));
    }

    [Fact]
    public void SetLanguage_InvalidYaml_UsesDefaults()
    {
        WriteYaml("broken.yaml", "this is: not: valid: yaml: [[[");

        var svc = new LocalizationService(_tempDir);
        svc.SetLanguage("broken");

        // Should not crash, should fallback
        Assert.Equal("Screen Translator", svc.T("app.title"));
    }
}
