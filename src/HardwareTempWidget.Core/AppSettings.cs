namespace HardwareTempWidget.Core;

public sealed class AppSettings
{
    public AppLanguage Language { get; set; } = AppLanguage.English;

    public double Opacity { get; set; } = 1.0;

    public string PanelBackgroundColor { get; set; } = "#CC1E1E28";

    public int PollIntervalMs { get; set; } = 1500;

    public double? WindowLeft { get; set; }

    public double? WindowTop { get; set; }

    public bool ShowCpuOnPanel { get; set; } = true;

    public bool ShowGpuOnPanel { get; set; } = true;

    public SensorType TrayIconMetric { get; set; } = SensorType.Cpu;

    public CpuDisplayMode CpuDisplayMode { get; set; } = CpuDisplayMode.Smoothing;

    public bool OverheatNotificationsEnabled { get; set; } = true;

    public int OverheatThresholdCelsius { get; set; } = 85;

    public double PanelFontSize { get; set; } = 15;
}
