using System.Management;

namespace HardwareTempWidget.Sensors.Windows;

/// <summary>
/// Reads sensor values from ASUS's own "AsusHWMonitorWMI" WMI provider (the same one MyASUS/Armoury
/// Crate use). Unlike LibreHardwareMonitorLib's WinRing0 driver, this goes through a vendor-signed
/// ACPI-WMI mapper that keeps working when Memory Integrity (HVCI) blocks direct MSR access.
/// Silently reports no readings on non-ASUS hardware or if the provider is missing.
/// </summary>
internal sealed class AsusWmiSensorReader
{
    private const string Namespace = "root\\wmi";
    private const string ClassName = "AsusHWMonitorWMI";

    private ManagementObject? _instance;
    private ManagementClass? _class;
    private bool _unavailable;

    public float? TryGetCpuTemperature()
    {
        if (!EnsureInstance())
        {
            return null;
        }

        try
        {
            using var idsResult = _instance!.InvokeMethod("getTotalSensorID", _class!.GetMethodParameters("getTotalSensorID"), null);
            var ids = (long[])idsResult["SensorIDArray"];
            var count = (long)idsResult["Size"];

            for (var i = 0; i < count && i < ids.Length; i++)
            {
                if (ids[i] == 0)
                {
                    continue;
                }

                using var limitParams = _class.GetMethodParameters("getSensorLimitByID");
                limitParams["ID"] = (uint)ids[i];
                using var limitResult = _instance.InvokeMethod("getSensorLimitByID", limitParams, null);

                var name = DecodeSensorName((byte[])limitResult["SensorName"]);
                if (name.Equals("CPU", StringComparison.OrdinalIgnoreCase))
                {
                    return Convert.ToSingle(limitResult["Value"]);
                }
            }

            return null;
        }
        catch
        {
            _unavailable = true;
            return null;
        }
    }

    private bool EnsureInstance()
    {
        if (_unavailable)
        {
            return false;
        }

        if (_instance is not null)
        {
            return true;
        }

        try
        {
            using var searcher = new ManagementObjectSearcher(Namespace, $"SELECT * FROM {ClassName} WHERE Active = true");
            using var results = searcher.Get();
            foreach (ManagementBaseObject result in results)
            {
                _instance = (ManagementObject)result;
                _class = new ManagementClass(Namespace, ClassName, null);
                return true;
            }

            _unavailable = true;
            return false;
        }
        catch
        {
            _unavailable = true;
            return false;
        }
    }

    private static string DecodeSensorName(byte[] raw)
    {
        var length = Array.IndexOf(raw, (byte)0);
        return System.Text.Encoding.ASCII.GetString(raw, 0, length < 0 ? raw.Length : length);
    }
}
