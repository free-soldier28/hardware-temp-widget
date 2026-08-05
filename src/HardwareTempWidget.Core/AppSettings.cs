namespace HardwareTempWidget.Core;

public sealed class AppSettings
{
    public double Opacity { get; set; } = 0.85;

    public int PollIntervalMs { get; set; } = 1500;

    public double? WindowLeft { get; set; }

    public double? WindowTop { get; set; }

    public bool ShowCpuOnPanel { get; set; } = true;

    public bool ShowGpuOnPanel { get; set; } = true;

    public SensorType TrayIconMetric { get; set; } = SensorType.Cpu;

    public bool OverheatNotificationsEnabled { get; set; } = true;

    public int OverheatThresholdCelsius { get; set; } = 85;
}
