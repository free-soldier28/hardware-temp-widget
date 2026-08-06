namespace HardwareTempWidget.Core.Tests;

public class AppVersionTests
{
    [Theory]
    [InlineData("v1.2.3")]
    [InlineData("1.2.3")]
    public void TryParse_AcceptsWithAndWithoutLeadingV(string tag)
    {
        Assert.True(AppVersion.TryParse(tag, out var version));
        Assert.Equal(new Version(1, 2, 3), version);
    }

    [Fact]
    public void TryParse_InvalidTag_ReturnsFalse()
    {
        Assert.False(AppVersion.TryParse("not-a-version", out _));
        Assert.False(AppVersion.TryParse("", out _));
    }

    [Theory]
    [InlineData("v1.2.3", 1, 2, 1)]
    [InlineData("v1.2.3", 1, 2, 3)]
    public void IsNewerOrEqual_TrueWhenLatestAtLeastCurrent(string tag, int maj, int min, int build)
    {
        Assert.True(AppVersion.IsNewerOrEqual(tag, new Version(maj, min, build)));
    }

    [Fact]
    public void IsNewerOrEqual_LatestOlderThanCurrent_ReturnsFalse()
    {
        Assert.False(AppVersion.IsNewerOrEqual("v1.2.3", new Version(2, 0, 0)));
    }

    [Fact]
    public void IsNewerOrEqual_InvalidTag_ReturnsFalse()
    {
        Assert.False(AppVersion.IsNewerOrEqual("junk", new Version(1, 0, 0)));
    }
}

public class GitHubReleaseParserTests
{
    private const string ZipAsset = "HardwareTempWidget-win-x64.zip";

    [Fact]
    public void ParseLatest_FindsMatchingZipAsset()
    {
        var json = $$"""
            {
              "tag_name": "v1.2.3",
              "assets": [
                { "name": "other.zip", "browser_download_url": "https://example.com/other.zip" },
                { "name": "{{ZipAsset}}", "browser_download_url": "https://example.com/download" }
              ]
            }
            """;

        var result = GitHubReleaseParser.ParseLatest(json, ZipAsset);

        Assert.NotNull(result);
        Assert.Equal(new Version(1, 2, 3), result.Version);
        Assert.Equal("https://example.com/download", result.ZipDownloadUrl);
    }

    [Fact]
    public void ParseLatest_NoMatchingAsset_ReturnsNull()
    {
        var json = $$"""
            { "tag_name": "v1.2.3", "assets": [ { "name": "other.zip", "browser_download_url": "https://example.com/other.zip" } ] }
            """;

        Assert.Null(GitHubReleaseParser.ParseLatest(json, ZipAsset));
    }

    [Fact]
    public void ParseLatest_NoTagName_ReturnsNull()
    {
        var json = """{ "assets": [] }""";

        Assert.Null(GitHubReleaseParser.ParseLatest(json, ZipAsset));
    }

    [Fact]
    public void ParseLatest_NonVersionTag_ReturnsNull()
    {
        var json = $$"""
            { "tag_name": "latest", "assets": [ { "name": "{{ZipAsset}}", "browser_download_url": "https://example.com/download" } ] }
            """;

        Assert.Null(GitHubReleaseParser.ParseLatest(json, ZipAsset));
    }

    [Fact]
    public void ParseLatest_NoAssets_ReturnsNull()
    {
        var json = """{ "tag_name": "v1.2.3" }""";

        Assert.Null(GitHubReleaseParser.ParseLatest(json, ZipAsset));
    }

    [Fact]
    public void ParseLatest_AssetWithoutUrl_ReturnsNull()
    {
        var json = $$"""
            { "tag_name": "v1.2.3", "assets": [ { "name": "{{ZipAsset}}" } ] }
            """;

        Assert.Null(GitHubReleaseParser.ParseLatest(json, ZipAsset));
    }

    [Fact]
    public void ParseLatest_InvalidJson_Throws()
    {
        Assert.ThrowsAny<Exception>(() => GitHubReleaseParser.ParseLatest("not json", ZipAsset));
    }

    [Fact]
    public void ParseLatest_RespectsAssetNameCase()
    {
        var json = $$"""
            { "tag_name": "v1.2.3", "assets": [ { "name": "HARDWARE-TEMP-WIDGET-WIN-X64.ZIP", "browser_download_url": "https://example.com/download" } ] }
            """;

        Assert.Null(GitHubReleaseParser.ParseLatest(json, ZipAsset));
    }
}