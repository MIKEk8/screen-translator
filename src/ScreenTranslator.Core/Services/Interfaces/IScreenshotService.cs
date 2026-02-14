using ScreenTranslator.Core.Models;

namespace ScreenTranslator.Core.Services.Interfaces;

public interface IScreenshotService
{
    ScreenshotResult CaptureRegion(ScreenRegion region);
    ScreenshotResult CaptureFullScreen();
}
