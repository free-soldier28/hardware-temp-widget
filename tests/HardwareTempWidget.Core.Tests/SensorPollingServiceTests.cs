namespace HardwareTempWidget.Core.Tests;

public class SensorPollingServiceTests
{
    private sealed class FakeProvider : ISensorProvider
    {
        public required Func<IReadOnlyList<SensorReading>> GetReadingsFunc { get; init; }
        public bool Disposed { get; private set; }

        public IReadOnlyList<SensorReading> GetReadings() => GetReadingsFunc();

        public void Dispose() => Disposed = true;
    }

    [Fact]
    public void Interval_SetAndGet_AreConsistent()
    {
        using var service = new SensorPollingService(new FakeProvider { GetReadingsFunc = () => [] }, TimeSpan.FromMilliseconds(100));

        service.Interval = TimeSpan.FromMilliseconds(250);

        Assert.Equal(TimeSpan.FromMilliseconds(250), service.Interval);
    }

    [Fact]
    public async Task Start_RaisesReadingsUpdatedOnEachTick()
    {
        var provider = new FakeProvider
        {
            GetReadingsFunc = () => new List<SensorReading>
            {
                new("CPU Package", SensorType.Cpu, 42f),
                new("GPU", SensorType.Gpu, 55f),
            },
        };

        var events = new List<IReadOnlyList<SensorReading>>();
        using var service = new SensorPollingService(provider, TimeSpan.FromMilliseconds(15));
        service.ReadingsUpdated += (_, readings) => events.Add(readings);

        service.Start();
        await Task.Delay(80);
        service.Stop();

        Assert.NotEmpty(events);
        Assert.All(events, r =>
        {
            Assert.Equal(2, r.Count);
            Assert.Contains(r, x => x.Type == SensorType.Cpu);
            Assert.Contains(r, x => x.Type == SensorType.Gpu);
        });
    }

    [Fact]
    public async Task Stop_StopsRaisingEvents()
    {
        var provider = new FakeProvider { GetReadingsFunc = () => [] };
        var events = 0;
        using var service = new SensorPollingService(provider, TimeSpan.FromMilliseconds(15));
        service.ReadingsUpdated += (_, _) => Interlocked.Increment(ref events);

        service.Start();
        await Task.Delay(40);
        service.Stop();
        await Task.Delay(60);

        Assert.NotEqual(0, Volatile.Read(ref events));
    }

    [Fact]
    public async Task Poll_ProviderThrows_DoesNotPropagateOrCrash()
    {
        var provider = new FakeProvider
        {
            GetReadingsFunc = () => throw new IOException("transient sensor failure"),
        };

        var events = 0;
        using var service = new SensorPollingService(provider, TimeSpan.FromMilliseconds(15));
        service.ReadingsUpdated += (_, _) => Interlocked.Increment(ref events);

        service.Start();
        await Task.Delay(60);
        service.Stop();

        Assert.Equal(0, Volatile.Read(ref events));
    }

    [Fact]
    public void Dispose_DisposesProvider()
    {
        var provider = new FakeProvider { GetReadingsFunc = () => [] };

        using (var service = new SensorPollingService(provider, TimeSpan.FromMilliseconds(100)))
        {
        }

        Assert.True(provider.Disposed);
    }
}