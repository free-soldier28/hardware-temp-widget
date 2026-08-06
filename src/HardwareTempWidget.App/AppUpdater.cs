using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using HardwareTempWidget.Core;

namespace HardwareTempWidget.App;

public sealed class AppUpdater : IAppUpdater
{
    private const string Repository = "free-soldier28/hardware-temp-widget";
    private const string ZipAssetName = "HardwareTempWidget-win-x64.zip";
    private const string ReleaseApiUrl = $"https://api.github.com/repos/{Repository}/releases/latest";

    private readonly HttpClient _http;
    private readonly string _appDirectory;
    private readonly string _executablePath;

    public AppUpdater(HttpClient? http = null, string? appDirectory = null, string? executablePath = null)
    {
        _http = http ?? new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd($"HardwareTempWidget/{VersionHelper.Current}");
        _appDirectory = appDirectory ?? AppContext.BaseDirectory;
        _executablePath = executablePath ?? Environment.ProcessPath ?? Path.Combine(_appDirectory, "HardwareTempWidget.App.exe");
    }

    public async Task<AppUpdateInfo?> CheckForUpdateAsync(Version currentVersion, CancellationToken ct = default)
    {
        try
        {
            var json = await _http.GetStringAsync(ReleaseApiUrl, ct).ConfigureAwait(false);
            var update = GitHubReleaseParser.ParseLatest(json, ZipAssetName);

            if (update is null || update.Version <= currentVersion)
            {
                return null;
            }

            return update;
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or TaskCanceledException)
        {
            return null;
        }
    }

    public async Task DownloadAndInstallAsync(
        AppUpdateInfo update,
        Action<double>? onProgress = null,
        CancellationToken ct = default)
    {
        var workDir = Path.Combine(Path.GetTempPath(), "HardwareTempWidget", $"update-{Environment.ProcessId}");
        Directory.CreateDirectory(workDir);

        var zipPath = Path.Combine(workDir, ZipAssetName);
        var extractDir = Path.Combine(workDir, "new");
        var scriptPath = Path.Combine(workDir, "apply.cmd");

        await DownloadZipAsync(update.ZipDownloadUrl, zipPath, onProgress, ct).ConfigureAwait(false);
        ZipFile.ExtractToDirectory(zipPath, extractDir);

        File.WriteAllText(scriptPath, BuildScript(zipPath, extractDir, _appDirectory, _executablePath));
        LaunchScript(scriptPath);
    }

    private async Task DownloadZipAsync(
        string url,
        string zipPath,
        Action<double>? onProgress,
        CancellationToken ct)
    {
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1;
        await using var source = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var target = File.Create(zipPath);
        var buffer = new byte[81920];
        long read = 0;
        int bytes;

        while ((bytes = await source.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, bytes), ct).ConfigureAwait(false);
            read += bytes;
            if (total > 0)
            {
                onProgress?.Invoke((double)read / total);
            }
        }
    }

    /// <summary>
    /// Writes a small cmd script that waits for the current process to exit, copies
    /// the freshly downloaded files over the app directory and relaunches the app.
    /// The script is launched detached so it survives our own exit.
    /// </summary>
    internal static string BuildScript(string zipPath, string extractDir, string appDirectory, string executablePath)
    {
        return string.Join(Environment.NewLine,
            "@echo off",
            "setlocal enabledelayedexpansion",
            "",
            ":wait",
            $"tasklist /FI \"IMAGENAME eq {Path.GetFileName(executablePath)}\" | find /I \"{Path.GetFileName(executablePath)}\" >nul",
            "if not errorlevel 1 (",
            "    timeout /t 1 /nobreak >nul",
            "    goto wait",
            ")",
            "",
            $"xcopy /E /Y /Q \"{extractDir}\\*.*\" \"{appDirectory}\" >nul",
            $"rmdir /s /q \"{extractDir}\" >nul 2>nul",
            $"del /q \"{zipPath}\" >nul 2>nul",
            "",
            $"start \"\" \"{executablePath}\"",
            "del /q \"%~f0\" >nul 2>nul",
            "exit /b 0");
    }

    private static void LaunchScript(string scriptPath)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"\"{scriptPath}\"\"",
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
        };
        System.Diagnostics.Process.Start(psi);
    }
}

internal static class VersionHelper
{
    public static Version Current =>
        typeof(AppUpdater).Assembly.GetName().Version ?? new Version(0, 0, 0);
}