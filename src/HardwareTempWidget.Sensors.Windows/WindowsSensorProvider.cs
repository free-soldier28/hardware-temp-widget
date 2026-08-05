using HardwareTempWidget.Core;
using LibreHardwareMonitor.Hardware;
using CoreSensorType = HardwareTempWidget.Core.SensorType;
using LhmSensorType = LibreHardwareMonitor.Hardware.SensorType;

namespace HardwareTempWidget.Sensors.Windows;

public sealed class WindowsSensorProvider : ISensorProvider
{
    private readonly Computer _computer;

    public WindowsSensorProvider()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
        };
        _computer.Open();
    }

    public IReadOnlyList<SensorReading> GetReadings()
    {
        var readings = new List<SensorReading>();

        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();

            CoreSensorType? sensorType = hardware.HardwareType switch
            {
                HardwareType.Cpu => CoreSensorType.Cpu,
                HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel => CoreSensorType.Gpu,
                _ => null,
            };

            if (sensorType is null)
            {
                continue;
            }

            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == LhmSensorType.Temperature && sensor.Value.HasValue)
                {
                    readings.Add(new SensorReading(sensor.Name, sensorType.Value, sensor.Value.Value));
                }
            }
        }

        return readings;
    }

    public void Dispose() => _computer.Close();
}
