# Screen Translator

Windows desktop app for real-time screen text capture, OCR recognition, and translation.

Select a screen region, the app recognizes text via Windows OCR and translates it using Google Translate or any OpenAI-compatible API (OpenRouter, LM Studio, Ollama, etc.).

![.NET 9](https://img.shields.io/badge/.NET-9.0-purple)
![Windows](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Screen capture** -- select any region on screen (multi-monitor support)
- **Windows OCR** -- native text recognition, no external dependencies
- **Translation providers:**
  - Google Translate (free, no API key)
  - OpenAI-compatible API (OpenRouter, GPT-4o, Claude, LM Studio, etc.)
  - Ollama (local LLMs)
- **OpenAI presets** -- save multiple model+prompt configurations
- **Text-to-Speech** -- Windows SAPI voices with adjustable rate/volume
- **Global hotkeys:**
  - `Alt+A` -- capture screen area
  - `Alt+C` -- copy selected text and translate
  - `Alt+X` -- stop speech
- **Auto-detect language** -- skips translation if text is already in target language
- **Overlay** -- shows translation on top of captured area

## Requirements

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Build & Run

```bash
# Run in development
dotnet run --project src/ScreenTranslator.App

# Build release
dotnet publish src/ScreenTranslator.App -c Release -r win-x64 --self-contained

# Run tests
dotnet test
```

## Project Structure

```
screen-translator/
├── src/
│   ├── ScreenTranslator.App/         # WPF application (UI)
│   │   ├── Pages/                    # TranslatePage, SettingsPage, AboutPage
│   │   ├── ViewModels/               # SettingsViewModel (MVVM)
│   │   └── Windows/                  # AreaSelectorWindow
│   │
│   └── ScreenTranslator.Core/        # Business logic
│       ├── Models/                    # AppConfig, OcrResult, etc.
│       └── Services/
│           ├── Interfaces/            # Service contracts
│           ├── Config/                # JSON config (auto-save)
│           ├── Hotkey/                # Global hotkeys (P/Invoke)
│           ├── Ocr/                   # Windows OCR (WinRT)
│           ├── Screenshot/            # Screen capture (GDI+)
│           ├── Translation/           # Google, OpenAI, Router
│           └── Tts/                   # Text-to-Speech (SAPI)
│
├── tests/
│   └── ScreenTranslator.Tests/
│
└── ScreenTranslator.sln
```

## Configuration

Settings are stored in `config.json` next to the executable. API keys are entered through the Settings page -- nothing is hardcoded.

To use OpenAI-compatible translation:
1. Go to Settings
2. Select provider "OpenAiCompatible"
3. Enter API endpoint (e.g. `https://openrouter.ai/api/v1`)
4. Enter API key
5. Click the refresh button to browse available models, or type model ID manually

## License

MIT
