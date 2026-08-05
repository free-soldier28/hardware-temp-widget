using HardwareTempWidget.Core;

namespace HardwareTempWidget.App;

internal static class PrimaryTemperatureSelector
{
    public static float? Select(IReadOnlyList<SensorReading> readings, SensorType type)
    {
        var candidates = readings.Where(r => r.Type == type).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var preferred = candidates.FirstOrDefault(r => r.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(r => r.Name.Contains("Hot Spot", StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(r =>
                r.Name.Contains("Core", StringComparison.OrdinalIgnoreCase)
                && !r.Name.Contains("Distance", StringComparison.OrdinalIgnoreCase));

        return (preferred ?? candidates[0]).TemperatureCelsius;
    }
}
