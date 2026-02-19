using ScreenTranslator.Core.Services.Hotkey;

namespace ScreenTranslator.Tests;

public class GlobalHotkeyServiceTests
{
    [Theory]
    [InlineData("Alt+A")]
    [InlineData("Ctrl+C")]
    [InlineData("Shift+F1")]
    [InlineData("Ctrl+Shift+X")]
    [InlineData("Alt+F12")]
    [InlineData("Win+D")]
    public void ValidateHotkey_ValidCombos_ReturnsTrue(string hotkey)
    {
        var (isValid, error) = GlobalHotkeyService.ValidateHotkey(hotkey);
        Assert.True(isValid, $"Expected valid but got error: {error}");
        Assert.Null(error);
    }

    [Fact]
    public void ValidateHotkey_Empty_ReturnsFalse()
    {
        var (isValid, error) = GlobalHotkeyService.ValidateHotkey("");
        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateHotkey_Null_ReturnsFalse()
    {
        var (isValid, error) = GlobalHotkeyService.ValidateHotkey(null);
        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateHotkey_NoModifier_ReturnsFalse()
    {
        var (isValid, error) = GlobalHotkeyService.ValidateHotkey("A");
        Assert.False(isValid);
        Assert.Contains("modifier", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateHotkey_NoKey_ReturnsFalse()
    {
        var (isValid, error) = GlobalHotkeyService.ValidateHotkey("Alt");
        Assert.False(isValid);
        Assert.Contains("key", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateHotkey_Whitespace_ReturnsFalse()
    {
        var (isValid, _) = GlobalHotkeyService.ValidateHotkey("   ");
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateHotkey_UnknownKey_ReturnsFalse()
    {
        var (isValid, error) = GlobalHotkeyService.ValidateHotkey("Alt+UNKNOWNKEY");
        Assert.False(isValid);
        Assert.Contains("Unknown", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Alt+F1", 0x70u)]
    [InlineData("Alt+F12", 0x7Bu)]
    [InlineData("Ctrl+F5", 0x74u)]
    public void ParseHotkey_FunctionKeys_ReturnsCorrectVk(string hotkey, uint expectedVk)
    {
        var (_, vk) = GlobalHotkeyService.ParseHotkey(hotkey);
        Assert.Equal(expectedVk, vk);
    }

    [Fact]
    public void ParseHotkey_StandardKey_ReturnsCorrectVk()
    {
        var (modifiers, vk) = GlobalHotkeyService.ParseHotkey("Alt+A");
        Assert.Equal(0x0001u, modifiers); // MOD_ALT
        Assert.Equal((uint)'A', vk);
    }
}
