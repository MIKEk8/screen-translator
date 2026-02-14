using ScreenTranslator.Core.Models;

namespace ScreenTranslator.Core.Services.Interfaces;

public interface IConfigService
{
    AppConfig Config { get; }
    Task SaveAsync();
    Task LoadAsync();
    event Action<AppConfig>? ConfigChanged;
}
