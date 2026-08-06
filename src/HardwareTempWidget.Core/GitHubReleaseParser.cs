using System.Text.Json;

namespace HardwareTempWidget.Core;

public static class GitHubReleaseParser
{
    private const string TagName = "tag_name";
    private const string Assets = "assets";
    private const string AssetName = "name";
    private const string DownloadUrl = "browser_download_url";

    /// <summary>
    /// Parses the GitHub "latest release" JSON payload and locates the zip asset
    /// matching <paramref name="zipAssetName"/>. Returns null when the release or
    /// asset is missing or the tag is not a version.
    /// </summary>
    public static AppUpdateInfo? ParseLatest(string json, string zipAssetName)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty(TagName, out var tag) || tag.ValueKind != JsonValueKind.String ||
            !AppVersion.TryParse(tag.GetString() ?? string.Empty, out var version))
        {
            return null;
        }

        if (!root.TryGetProperty(Assets, out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var asset in assets.EnumerateArray())
        {
            if (!asset.TryGetProperty(AssetName, out var name) || name.ValueKind != JsonValueKind.String ||
                name.GetString() != zipAssetName)
            {
                continue;
            }

            if (!asset.TryGetProperty(DownloadUrl, out var url) || url.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return new AppUpdateInfo(version, url.GetString()!);
        }

        return null;
    }
}