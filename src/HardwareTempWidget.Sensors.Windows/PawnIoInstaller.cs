using System.Diagnostics;
using System.Net.Http;
using Microsoft.Win32;

namespace HardwareTempWidget.Sensors.Windows;

/// <summary>
/// Installs PawnIO (https://pawnio.eu) — the open-source, HVCI-signed kernel driver that
/// LibreHardwareMonitorLib 0.9.5+ uses in place of WinRing0 for per-core MSR temperature reads.
/// </summary>
public static class PawnIoInstaller
{
    private const string DownloadUrl = "https://github.com/namazso/PawnIO.Setup/releases/latest/download/PawnIO_setup.exe";

    public static bool IsInstalled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\PawnIO");
            return key is not null;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> InstallAsync()
    {
        try
        {
            var installerPath = Path.Combine(Path.GetTempPath(), "PawnIO_setup.exe");

            using (var http = new HttpClient())
            await using (var source = await http.GetStreamAsync(DownloadUrl))
            await using (var destination = File.Create(installerPath))
            {
                await source.CopyToAsync(destination);
            }

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "-install -silent",
                UseShellExecute = true,
                Verb = "runas",
            });

            if (process is null)
            {
                return false;
            }

            await process.WaitForExitAsync();
            return IsInstalled();
        }
        catch
        {
            return false;
        }
    }
}
