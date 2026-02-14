using System.Runtime.InteropServices;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Core.Services.Hotkey;

public partial class GlobalHotkeyService : IHotkeyService
{
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    private readonly Dictionary<int, Action> _registeredHotkeys = [];
    private int _nextId = 1;
    private IntPtr _windowHandle;

    public void SetWindowHandle(IntPtr handle)
    {
        _windowHandle = handle;
    }

    public void Register(string hotkey, Action callback)
    {
        var (modifiers, vk) = ParseHotkey(hotkey);
        int id = _nextId++;

        if (!RegisterHotKey(_windowHandle, id, modifiers | MOD_NOREPEAT, vk))
        {
            throw new InvalidOperationException($"Failed to register hotkey: {hotkey}");
        }

        _registeredHotkeys[id] = callback;
    }

    public void Unregister(string hotkey)
    {
        // Simplified — unregisters all for now
        UnregisterAll();
    }

    public void UnregisterAll()
    {
        foreach (var id in _registeredHotkeys.Keys)
        {
            UnregisterHotKey(_windowHandle, id);
        }
        _registeredHotkeys.Clear();
    }

    public bool HandleMessage(int id)
    {
        if (_registeredHotkeys.TryGetValue(id, out var callback))
        {
            callback();
            return true;
        }
        return false;
    }

    public void Dispose()
    {
        UnregisterAll();
        GC.SuppressFinalize(this);
    }

    private static (uint modifiers, uint vk) ParseHotkey(string hotkey)
    {
        uint modifiers = 0;
        uint vk = 0;

        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            switch (part.ToUpperInvariant())
            {
                case "ALT": modifiers |= MOD_ALT; break;
                case "CTRL" or "CONTROL": modifiers |= MOD_CONTROL; break;
                case "SHIFT": modifiers |= MOD_SHIFT; break;
                case "WIN": modifiers |= MOD_WIN; break;
                default:
                    // Single character key
                    if (part.Length == 1 && char.IsLetterOrDigit(part[0]))
                        vk = (uint)char.ToUpperInvariant(part[0]);
                    break;
            }
        }

        return (modifiers, vk);
    }
}
