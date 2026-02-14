# CLAUDE.md — Screen Translator

## Stack

- **UI**: WPF (.NET 9, net9.0-windows10.0.22621.0)
- **Core**: C# class library
- **DI**: Microsoft.Extensions.DependencyInjection
- **MVVM**: CommunityToolkit.Mvvm
- **Tests**: xUnit

## Commands

```bash
dotnet build              # Build
dotnet test               # Tests
dotnet run --project src/ScreenTranslator.App   # Run
dotnet publish src/ScreenTranslator.App -c Release -r win-x64 --self-contained  # Release
```

## Structure

```
src/
├── ScreenTranslator.App/        # WPF app (UI, ViewModels, Pages)
└── ScreenTranslator.Core/       # Business logic (Models, Services)
tests/
└── ScreenTranslator.Tests/      # xUnit tests
```

## Patterns

- **DI** -- all services via `IServiceCollection`
- **Interfaces** -- every service has an interface (`IOcrService`, `ITranslationService`)
- **TranslationRouter** -- delegates to current provider based on config (not cached for OpenAI/Ollama)
- **Auto-save** -- settings save on change with 500ms debounce (`ScheduleAutoSave`)
- **`_isLoading` flag** -- prevents change handlers during `LoadFromConfig()`
- **P/Invoke** -- for Windows API (hotkeys, screen metrics)
- **WinRT** -- for Windows OCR (`Windows.Media.Ocr`)

## Gotchas

- **TFM**: all projects must be `net9.0-windows10.0.22621.0` (not just `net9.0`)
- **AllowUnsafeBlocks**: needed in Core for `LibraryImport` (P/Invoke source gen)
- **WinRT alias**: `using WinOcr = Windows.Media.Ocr` -- avoid conflict with our `OcrResult`
- **System.Drawing.Common**: needs NuGet package for `Bitmap`
- **XAML LineHeight**: cannot be set on TextBox, only on TextBlock
- **TolerantEnumConverter**: config deserialization won't crash on unknown enum values
