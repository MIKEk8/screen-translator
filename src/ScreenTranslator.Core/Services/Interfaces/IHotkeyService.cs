namespace ScreenTranslator.Core.Services.Interfaces;

public interface IHotkeyService : IDisposable
{
    void Register(string hotkey, Action callback);
    void Unregister(string hotkey);
    void UnregisterAll();
}
