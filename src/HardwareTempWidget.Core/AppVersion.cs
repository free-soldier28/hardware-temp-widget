namespace HardwareTempWidget.Core;

public static class AppVersion
{
    public static bool TryParse(string tag, out Version version)
    {
        var text = tag.StartsWith('v') ? tag[1..] : tag;
        return Version.TryParse(text, out version);
    }

    public static bool IsNewerOrEqual(string latestTag, Version current) =>
        TryParse(latestTag, out var latest) && latest >= current;
}