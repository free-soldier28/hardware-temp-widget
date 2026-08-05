namespace HardwareTempWidget.Core;

public interface ISensorProvider : IDisposable
{
    IReadOnlyList<SensorReading> GetReadings();
}
