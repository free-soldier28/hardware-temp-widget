using HardwareTempWidget.Core;
using HardwareTempWidget.Sensors.Windows;

namespace HardwareTempWidget.App;

internal static class OverheatNotifierFactory
{
    public static IOverheatNotifier Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsToastNotifier();
        }

        throw new PlatformNotSupportedException("No overheat notifier is available for this platform yet.");
    }
}
