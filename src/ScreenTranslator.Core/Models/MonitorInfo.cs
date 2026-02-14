namespace ScreenTranslator.Core.Models;

public record MonitorInfo(
    string DeviceName,
    int Left,
    int Top,
    int Width,
    int Height,
    bool IsPrimary,
    double ScaleFactor
);

public record VirtualDesktopBounds(
    int Left,
    int Top,
    int Width,
    int Height
);
