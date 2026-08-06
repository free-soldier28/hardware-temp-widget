using System.Text.RegularExpressions;
using HardwareTempWidget.Core;

namespace HardwareTempWidget.App;

internal static partial class CpuCoreReadings
{
    public static List<SensorReading> Extract(IReadOnlyList<SensorReading> readings) =>
        readings
            .Where(r => r.Type == SensorType.Cpu
                && r.Name.Contains("Core #", StringComparison.OrdinalIgnoreCase)
                && !r.Name.Contains("Distance", StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => CorePrefix(r.Name), StringComparer.OrdinalIgnoreCase)
            .ThenBy(CoreNumber)
            .ToList();

    public static List<(string Type, List<SensorReading> Cores)> GroupByType(IReadOnlyList<SensorReading> readings) =>
        Extract(readings)
            .GroupBy(r => CorePrefix(r.Name).Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => (Type: DisplayName(g.Key), Cores: g.OrderBy(CoreNumber).ToList()))
            .ToList();

    private static string DisplayName(string prefix) => prefix.Trim().TrimEnd('-').ToUpperInvariant() switch
    {
        "P" => Localization.T("CoreType.Performance"),
        "E" => Localization.T("CoreType.Efficiency"),
        _ => prefix,
    };

    private static string CorePrefix(string name)
    {
        var index = name.IndexOf("Core #", StringComparison.OrdinalIgnoreCase);
        return index < 0 ? name : name[..index];
    }

    public static int CoreNumber(SensorReading reading)
    {
        var match = CoreNumberRegex().Match(reading.Name);
        return match.Success ? int.Parse(match.Groups[1].Value) : int.MaxValue;
    }

    [GeneratedRegex(@"Core #(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex CoreNumberRegex();
}
