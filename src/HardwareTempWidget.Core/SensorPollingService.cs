using Timer = System.Timers.Timer;

namespace HardwareTempWidget.Core;

public sealed class SensorPollingService : IDisposable
{
    private readonly ISensorProvider _provider;
    private readonly Timer _timer;

    public event EventHandler<IReadOnlyList<SensorReading>>? ReadingsUpdated;

    public SensorPollingService(ISensorProvider provider, TimeSpan interval)
    {
        _provider = provider;
        _timer = new Timer(interval.TotalMilliseconds) { AutoReset = true };
        _timer.Elapsed += (_, _) => Poll();
    }

    public TimeSpan Interval
    {
        get => TimeSpan.FromMilliseconds(_timer.Interval);
        set => _timer.Interval = value.TotalMilliseconds;
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    private void Poll()
    {
        try
        {
            var readings = _provider.GetReadings();
            ReadingsUpdated?.Invoke(this, readings);
        }
        catch
        {
            // Transient sensor read failures are ignored; the next tick retries.
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        _provider.Dispose();
    }
}
