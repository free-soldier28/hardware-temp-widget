using HardwareTempWidget.Core;
using HardwareTempWidget.Sensors.Windows;

namespace HardwareTempWidget.App;

internal static class SensorProviderFactory
{
    public static ISensorProvider Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsSensorProvider();
        }

        throw new PlatformNotSupportedException("No sensor provider is available for this platform yet.");
    }
}
