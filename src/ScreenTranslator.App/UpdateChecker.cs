using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace ScreenTranslator.App;

public static class UpdateChecker
{
    private const string ApiUrl = "https://api.github.com/repos/MIKEk8/screen-translator/releases/latest";

    public static UpdateInfo? LatestUpdate { get; private set; }

    private static Timer? _timer;
    private static Action<UpdateInfo?>? _callback;

    public static void Start(Action<UpdateInfo?> onResult)
    {
        _callback = onResult;
        // Initial check after 5 seconds, then every hour
        _timer = new Timer(_ => _ = CheckInternalAsync(), null, TimeSpan.FromSeconds(5), TimeSpan.FromHours(1));
    }

    public static async Task<UpdateInfo?> CheckNowAsync()
    {
        var result = await CheckAsync();
        if (result is not null)
            LatestUpdate = result;
        return result;
    }

    private static async Task CheckInternalAsync()
    {
        var result = await CheckAsync();
        if (result is not null)
            LatestUpdate = result;
        _callback?.Invoke(result);
    }

    private static async Task<UpdateInfo?> CheckAsync()
    {
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "ScreenTranslator");
            var json = await http.GetStringAsync(ApiUrl);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString() ?? "";
            var htmlUrl = root.GetProperty("html_url").GetString() ?? "";

            // Find .zip asset
            string? assetUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        assetUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }
            }

            if (assetUrl is null) return null;

            var remoteStr = tagName.TrimStart('v');
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            if (!Version.TryParse(remoteStr, out var remoteVersion) || currentVersion is null)
                return null;

            var current = new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build);
            var remote = new Version(remoteVersion.Major, remoteVersion.Minor,
                remoteVersion.Build >= 0 ? remoteVersion.Build : 0);

            if (remote > current)
                return new UpdateInfo(tagName, remote, htmlUrl, assetUrl);

            return null;
        }
        catch
        {
            return null;
        }
    }
}
