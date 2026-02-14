namespace ScreenTranslator.Core.Models;

public record ScreenshotResult(
    byte[] ImageData,
    int Width,
    int Height,
    ScreenRegion Region
);

public record ScreenRegion(
    int X,
    int Y,
    int Width,
    int Height
);
