namespace HardwareTempWidget.Core;

public sealed record SensorReading(string Name, SensorType Type, float TemperatureCelsius);
