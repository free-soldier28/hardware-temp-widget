using HardwareTempWidget.Core;
using HardwareTempWidget.Sensors.Windows;

namespace HardwareTempWidget.App;

internal static class AutostartServiceFactory
{
    public static IAutostartService Create()
    {
        if (OperatingSystem.IsWindows())
        {
            var executablePath = Environment.ProcessPath ?? Environment.GetCommandLineArgs()[0];
            return new WindowsAutostartService(executablePath);
        }

        throw new PlatformNotSupportedException("No autostart service is available for this platform yet.");
    }
}
