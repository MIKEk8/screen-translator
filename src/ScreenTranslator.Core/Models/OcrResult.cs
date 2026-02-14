namespace ScreenTranslator.Core.Models;

public record OcrResult(
    string Text,
    string Language,
    float Confidence,
    List<OcrTextBlock> Blocks
);

public record OcrTextBlock(
    string Text,
    OcrBoundingBox BoundingBox
);

public record OcrBoundingBox(
    double X,
    double Y,
    double Width,
    double Height
);
