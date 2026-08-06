namespace HardwareTempWidget.App.Tests;

public class AppUpdaterTests
{
    [Fact]
    public void BuildScript_ContainsCopyStartAndCleanup()
    {
        var script = AppUpdater.BuildScript(
            @"C:\temp\update.zip",
            @"C:\temp\new",
            @"C:\Program Files\HardwareTempWidget",
            @"C:\Program Files\HardwareTempWidget\HardwareTempWidget.App.exe");

        Assert.Contains("tasklist", script);
        Assert.Contains("xcopy /E /Y /Q", script);
        Assert.Contains("start", script);
        Assert.Contains(@"new\*.*", script);
    }

    [Fact]
    public void BuildScript_QuotesPathsWithSpaces()
    {
        var script = AppUpdater.BuildScript(
            @"C:\temp\app.zip",
            "C:\\temp\\new dir",
            "C:\\Program Files\\HardwareTempWidget",
            "C:\\Program Files\\HardwareTempWidget\\HardwareTempWidget.App.exe");

        Assert.Contains("\"C:\\temp\\new dir\\*.*\"", script);
        Assert.Contains("\"C:\\Program Files\\HardwareTempWidget\"", script);
    }

    [Fact]
    public void VersionHelper_CurrentIsParsableVersion()
    {
        Assert.True(VersionHelper.Current.Major >= 0);
    }
}