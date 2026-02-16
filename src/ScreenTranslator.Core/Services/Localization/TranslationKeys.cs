namespace ScreenTranslator.Core.Services.Localization;

/// <summary>
/// Single entry: default English text + version when this key was last modified.
/// </summary>
public record TranslationEntry(string Default, string Since);

/// <summary>
/// Reference dictionary of ALL translatable keys.
/// When a key's text changes meaning, bump its <c>Since</c> version.
/// </summary>
public static class TranslationKeys
{
    public const string CurrentVersion = "1.1.0";

    public static readonly Dictionary<string, TranslationEntry> All = new()
    {
        // ── Main window ──
        ["app.title"]                    = new("Screen Translator", "1.0.0"),
        ["nav.translate"]                = new("Translate", "1.0.0"),
        ["nav.preview"]                  = new("Capture Preview", "1.0.0"),
        ["nav.settings"]                 = new("Settings", "1.0.0"),
        ["nav.about"]                    = new("About", "1.0.0"),
        ["tray.show"]                    = new("Show", "1.0.0"),
        ["tray.exit"]                    = new("Exit", "1.0.0"),
        ["app.already_running"]          = new("Screen Translator is already running.", "1.0.0"),

        // ── Translate page ──
        ["translate.source"]             = new("Source", "1.0.0"),
        ["translate.translation"]        = new("Translation", "1.0.0"),
        ["translate.capture"]            = new("Capture", "1.0.0"),
        ["translate.translate"]          = new("Translate", "1.0.0"),
        ["translate.speak_source"]       = new("Speak source text", "1.0.0"),
        ["translate.speak_target"]       = new("Speak translation", "1.0.0"),
        ["translate.ocr_engine"]         = new("OCR engine", "1.1.0"),
        ["translate.provider"]           = new("Translation provider", "1.1.0"),

        // ── Translate page — OCR options ──
        ["ocr.windows"]                  = new("Windows OCR", "1.1.0"),
        ["ocr.tesseract"]                = new("Tesseract", "1.1.0"),

        // ── Translate page — status ──
        ["status.ready"]                 = new("Ready", "1.0.0"),
        ["status.recognizing"]           = new("Recognizing text...", "1.0.0"),
        ["status.translating"]           = new("Translating...", "1.0.0"),
        ["status.translating_vision"]    = new("Translating image (vision)...", "1.1.0"),
        ["status.speaking"]              = new("Speaking...", "1.0.0"),
        ["status.no_text"]               = new("No text detected", "1.0.0"),
        ["status.no_clipboard"]          = new("No text in clipboard", "1.0.0"),
        ["status.already_target"]        = new("Already in {0} — skipped", "1.0.0"),
        ["status.error"]                 = new("Error: {0}", "1.0.0"),
        ["status.translation_error"]     = new("Translation error: {0}", "1.0.0"),
        ["status.tts_error"]             = new("TTS error: {0}", "1.0.0"),

        // ── Preview page ──
        ["preview.title"]                = new("Capture Preview", "1.0.0"),
        ["preview.capture"]              = new("Capture Area", "1.0.0"),
        ["preview.no_capture"]           = new("No capture yet", "1.0.0"),
        ["preview.hint"]                 = new("Click 'Capture Area' or press Alt+A", "1.0.0"),
        ["preview.fit"]                  = new("Fit", "1.0.0"),
        ["preview.recognizing"]          = new("Recognizing...", "1.0.0"),
        ["preview.no_text_ocr"]          = new("OCR: no text detected", "1.0.0"),

        // ── Settings page ──
        ["settings.title"]               = new("Settings", "1.0.0"),
        ["settings.translation"]         = new("TRANSLATION", "1.0.0"),
        ["settings.provider"]            = new("Provider", "1.0.0"),
        ["settings.ocr_engine"]          = new("OCR Engine", "1.0.0"),
        ["settings.openai_title"]        = new("OpenAI Compatible Settings", "1.0.0"),
        ["settings.preset"]              = new("Preset", "1.0.0"),
        ["settings.add_preset"]          = new("Add preset", "1.0.0"),
        ["settings.remove_preset"]       = new("Remove preset", "1.0.0"),
        ["settings.name"]                = new("Name", "1.0.0"),
        ["settings.endpoint"]            = new("Endpoint", "1.0.0"),
        ["settings.api_key"]             = new("API Key", "1.0.0"),
        ["settings.model"]               = new("Model", "1.0.0"),
        ["settings.fetch_models"]        = new("Fetch models from API", "1.0.0"),
        ["settings.system_prompt"]       = new("System Prompt (optional, {source}/{target} placeholders)", "1.0.0"),
        ["settings.vision_mode"]         = new("Vision mode (send screenshot directly, skip OCR)", "1.1.0"),
        ["settings.ollama_title"]        = new("Ollama Settings", "1.0.0"),

        // ── Settings — TTS ──
        ["settings.tts"]                 = new("TEXT-TO-SPEECH", "1.0.0"),
        ["settings.voice"]               = new("Voice", "1.0.0"),
        ["settings.speed"]               = new("Speed", "1.0.0"),
        ["settings.volume"]              = new("Volume", "1.0.0"),
        ["settings.auto_speak"]          = new("Auto-speak translation", "1.0.0"),
        ["settings.test"]                = new("Test", "1.0.0"),
        ["settings.notification_volume"] = new("Notification", "1.1.0"),

        // ── Settings — Hotkeys ──
        ["settings.hotkeys"]             = new("HOTKEYS", "1.0.0"),
        ["settings.hotkey_capture"]      = new("Capture", "1.0.0"),
        ["settings.hotkey_copy"]         = new("Copy & Translate", "1.0.0"),
        ["settings.hotkey_stop"]         = new("Stop Speech", "1.0.0"),

        // ── Settings — Mouse gesture ──
        ["settings.gesture"]             = new("MOUSE GESTURE", "1.1.0"),
        ["settings.gesture_enable"]      = new("Enable mouse gesture capture", "1.1.0"),
        ["settings.gesture_button"]      = new("Mouse button", "1.1.0"),
        ["settings.gesture_hint"]        = new("Hold the mouse button and draw a circle around text to capture and translate", "1.1.0"),

        // ── Settings — Overlay ──
        ["settings.overlay"]             = new("OVERLAY", "1.0.0"),
        ["settings.overlay_show"]        = new("Show overlay on translate", "1.0.0"),
        ["settings.opacity"]             = new("Opacity", "1.0.0"),
        ["settings.font_size"]           = new("Font size", "1.0.0"),

        // ── Settings — Application ──
        ["settings.application"]         = new("APPLICATION", "1.1.0"),
        ["settings.autostart"]           = new("Launch at Windows startup", "1.1.0"),
        ["settings.start_minimized"]     = new("Start minimized to tray", "1.1.0"),

        // ── Settings — Language ──
        ["settings.language"]            = new("LANGUAGE", "1.1.0"),
        ["settings.interface_lang"]      = new("Interface language", "1.1.0"),
        ["settings.validate"]            = new("Validate translations", "1.1.0"),
        ["settings.reset"]               = new("Reset to Defaults", "1.0.0"),
        ["settings.saved"]               = new("Saved", "1.0.0"),

        // ── About page ──
        ["about.title"]                  = new("About", "1.0.0"),
        ["about.description"]            = new("Desktop app for screen capture, OCR text recognition, and real-time translation.", "1.0.0"),
        ["about.build_time"]             = new("Build time", "1.0.0"),
        ["about.runtime"]                = new("Runtime", "1.0.0"),
        ["about.platform"]               = new("Platform", "1.0.0"),
        ["about.technology"]             = new("TECHNOLOGY", "1.0.0"),
        ["about.hotkeys"]                = new("HOTKEYS", "1.0.0"),
        ["about.hotkey_capture"]         = new("Capture screen area and translate", "1.1.0"),
        ["about.hotkey_copy"]            = new("Copy selected text and translate", "1.0.0"),
        ["about.hotkey_stop"]            = new("Stop speech", "1.0.0"),
        ["about.gesture"]                = new("MOUSE GESTURE", "1.1.0"),
        ["about.gesture_desc"]           = new("Hold a mouse side button (XButton) and draw a circle or oval around text on screen. The enclosed area will be captured, recognized, and translated automatically.", "1.1.0"),
        ["about.gesture_config"]         = new("Configure the mouse button and enable/disable in Settings.", "1.1.0"),
        ["about.check_updates"]          = new("Check for updates", "1.1.0"),
        ["about.update_available"]       = new("{0} available", "1.1.0"),
        ["about.up_to_date"]             = new("Up to date", "1.1.0"),
        ["about.update_error"]           = new("Could not check for updates", "1.1.0"),
        ["about.download"]              = new("Download", "1.1.0"),
        ["about.update"]                = new("Update", "1.1.0"),
        ["about.updating"]              = new("Updating...", "1.1.0"),
        ["about.footer"]                 = new("Personal project", "1.0.0"),

        // ── Area selector ──
        ["selector.hint"]                = new("Draw a rectangle to select area. Press ESC to cancel.", "1.0.0"),

        // ── Validation ──
        ["validation.title"]             = new("Translation Validation", "1.1.0"),
        ["validation.ok"]                = new("All keys are translated and up to date.", "1.1.0"),
        ["validation.missing"]           = new("Missing (not translated):", "1.1.0"),
        ["validation.outdated"]          = new("Outdated (text changed in v{0}):", "1.1.0"),
        ["validation.deprecated"]        = new("Deprecated (can be removed):", "1.1.0"),
        ["validation.yaml_error"]        = new("YAML syntax error at line {0}: {1}", "1.1.0"),
    };
}
