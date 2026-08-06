namespace HardwareTempWidget.Core;

public interface IAppUpdater
{
    Task<AppUpdateInfo?> CheckForUpdateAsync(Version currentVersion, CancellationToken ct = default);

    Task DownloadAndInstallAsync(AppUpdateInfo update, Action<double>? onProgress = null, CancellationToken ct = default);
}