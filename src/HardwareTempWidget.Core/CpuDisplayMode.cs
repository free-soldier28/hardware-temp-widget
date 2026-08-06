namespace HardwareTempWidget.Core;

/// <summary>
/// Controls how the CPU temperature is derived from the available sensor readings.
/// </summary>
public enum CpuDisplayMode
{
    /// <summary>Moving average over recent readings to smooth out transient spikes.</summary>
    Smoothing,

    /// <summary>Uses the "CPU Core Average" sensor when available.</summary>
    CoreAverage,

    /// <summary>Legacy behavior: Package → Hot Spot → first core reading.</summary>
    Default,
}
