using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace ScreenTranslator.App.Pages;

public partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        Loaded += (_, _) => PopulateInfo();
    }

    private void PopulateInfo()
    {
        var asm = Assembly.GetExecutingAssembly();
        var version = asm.GetName().Version;

        VersionText.Text = $"v{version?.Major}.{version?.Minor}.{version?.Build}";

        // Build time = DLL last write time
        var location = asm.Location;
        if (!string.IsNullOrEmpty(location) && File.Exists(location))
        {
            var buildTime = File.GetLastWriteTime(location);
            BuildTimeText.Text = buildTime.ToString("yyyy-MM-dd HH:mm");
        }
        else
        {
            BuildTimeText.Text = "N/A";
        }

        RuntimeText.Text = $".NET {Environment.Version}";
        PlatformText.Text = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";
    }
}
