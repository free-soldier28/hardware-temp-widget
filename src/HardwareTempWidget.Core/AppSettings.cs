namespace HardwareTempWidget.Core;

public sealed class AppSettings
{
    public double Opacity { get; set; } = 0.85;

    public int PollIntervalMs { get; set; } = 1500;

    public double? WindowLeft { get; set; }

    public double? WindowTop { get; set; }
}
