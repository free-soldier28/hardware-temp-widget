using HardwareTempWidget.Core;

namespace HardwareTempWidget.App.Tests;

public class CpuCoreReadingsTests
{
    private static SensorReading Core(string name, float temp) => new(name, SensorType.Cpu, temp);

    [Fact]
    public void Extract_ReturnsOnlyCpuCoreReadings_OrderedByPrefixThenCoreNumber()
    {
        var readings = new List<SensorReading>
        {
            new("GPU Hot Spot", SensorType.Gpu, 70f),
            Core("CPU Core #10", 50f),
            Core("CCD1 Core #2", 45f),
            Core("CPU Core #1", 40f),
            Core("CPU Core Distance to TjMax", 30f),
        };

        var result = CpuCoreReadings.Extract(readings);

        Assert.Collection(
            result,
            r => Assert.Equal("CCD1 Core #2", r.Name),
            r => Assert.Equal("CPU Core #1", r.Name),
            r => Assert.Equal("CPU Core #10", r.Name));
    }

    [Fact]
    public void Extract_ExcludesDistanceToTjMax()
    {
        var result = CpuCoreReadings.Extract(new List<SensorReading>
        {
            Core("CPU Core #1", 40f),
            Core("CPU Core #1 Distance to TjMax", 10f),
        });

        Assert.Single(result);
        Assert.Equal("CPU Core #1", result[0].Name);
    }

    [Fact]
    public void Extract_NoCores_ReturnsEmpty()
    {
        var result = CpuCoreReadings.Extract(new List<SensorReading>
        {
            new("GPU", SensorType.Gpu, 70f),
            new("CPU Package", SensorType.Cpu, 60f),
        });

        Assert.Empty(result);
    }

    [Fact]
    public void Extract_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(CpuCoreReadings.Extract([]));
    }

    [Fact]
    public void Extract_OrdersNumericallyNotLexically()
    {
        var result = CpuCoreReadings.Extract(new List<SensorReading>
        {
            Core("CPU Core #10", 1f),
            Core("CPU Core #2", 2f),
            Core("CPU Core #1", 3f),
        });

        Assert.Collection(
            result,
            r => Assert.Equal("CPU Core #1", r.Name),
            r => Assert.Equal("CPU Core #2", r.Name),
            r => Assert.Equal("CPU Core #10", r.Name));
    }
}

public class CpuCoreGroupingTests
{
    private static SensorReading Core(string name, float temp) => new(name, SensorType.Cpu, temp);

    [Fact]
    public void GroupByType_WithTwoCoreTypes_GroupsByPrefix()
    {
        var readings = new List<SensorReading>
        {
            Core("CCD1 Core #0", 50f),
            Core("CCD0 Core #1", 46f),
            Core("CCD0 Core #0", 45f),
            Core("CCD1 Core #1", 52f),
        };

        var result = CpuCoreReadings.GroupByType(readings);

        Assert.Equal(2, result.Count);
        Assert.Equal("CCD0", result[0].Type);
        Assert.Equal("CCD1", result[1].Type);
        Assert.Equal(new[] { 0, 1 }, result[0].Cores.Select(CpuCoreReadings.CoreNumber));
        Assert.Equal(new[] { 0, 1 }, result[1].Cores.Select(CpuCoreReadings.CoreNumber));
    }

    [Fact]
    public void GroupByType_NoCores_ReturnsEmpty()
    {
        Assert.Empty(CpuCoreReadings.GroupByType(new List<SensorReading>
        {
            new("GPU", SensorType.Gpu, 70f),
            new("CPU Package", SensorType.Cpu, 60f),
        }));
    }

    [Fact]
    public void GroupByType_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(CpuCoreReadings.GroupByType([]));
    }

    [Fact]
    public void GroupByType_MapsKnownCoreTypeAbbreviations()
    {
        var result = CpuCoreReadings.GroupByType(new List<SensorReading>
        {
            Core("P-Core #1", 51f),
            Core("E-Core #2", 40f),
            Core("P-Core #2", 52f),
        });

        Assert.Equal(2, result.Count);
        Assert.Equal("Efficiency (E)", result[0].Type);
        Assert.Equal("Performance (P)", result[1].Type);
    }

    [Fact]
    public void GroupByType_KeepsUnknownPrefixesVerbatim()
    {
        var result = CpuCoreReadings.GroupByType(new List<SensorReading>
        {
            Core("CCD0 Core #2", 45f),
        });

        Assert.Equal("CCD0", result[0].Type);
    }

    [Fact]
    public void GroupByType_OrdersCoresNumerically()
    {
        var result = CpuCoreReadings.GroupByType(new List<SensorReading>
        {
            Core("CPU Core #10", 1f),
            Core("CPU Core #2", 2f),
            Core("CPU Core #1", 3f),
        });

        Assert.Equal("CPU", result[0].Type);
        Assert.Equal(new[] { 1, 2, 10 }, result[0].Cores.Select(CpuCoreReadings.CoreNumber));
    }
}

public class PrimaryTemperatureSelectorTests
{
    private static SensorReading Reading(string name, SensorType type, float temp) => new(name, type, temp);

    [Fact]
    public void Select_NoReadingsOfType_ReturnsNull()
    {
        var readings = new List<SensorReading> { Reading("CPU Package", SensorType.Cpu, 40f) };

        Assert.Null(PrimaryTemperatureSelector.Select(readings, SensorType.Gpu));
    }

    [Theory]
    [InlineData("CPU Package", 42f)]
    [InlineData("Core Temperature #0 Package", 43f)]
    public void Select_Cpu_PrefersPackageOverCores(string name, float temp)
    {
        var readings = new List<SensorReading>
        {
            Reading("CPU Core #4", SensorType.Cpu, 44f),
            Reading("CPU (Tctl/Tdie)", SensorType.Cpu, 43f),
            Reading(name, SensorType.Cpu, temp),
        };

        var result = PrimaryTemperatureSelector.Select(readings, SensorType.Cpu);

        Assert.Equal(temp, result);
    }

    [Fact]
    public void Select_Gpu_PrefersHotSpot()
    {
        var readings = new List<SensorReading>
        {
            Reading("GPU Core", SensorType.Gpu, 60f),
            Reading("GPU Hot Spot", SensorType.Gpu, 82f),
            Reading("GPU Memory Junction", SensorType.Gpu, 90f),
        };

        var result = PrimaryTemperatureSelector.Select(readings, SensorType.Gpu);

        Assert.Equal(82f, result);
    }

    [Fact]
    public void Select_Gpu_FallsBackToHotSpotWhenAbsent()
    {
        var readings = new List<SensorReading>
        {
            Reading("GPU Core", SensorType.Gpu, 60f),
            Reading("GPU Memory Junction", SensorType.Gpu, 90f),
        };

        var result = PrimaryTemperatureSelector.Select(readings, SensorType.Gpu);

        Assert.Equal(60f, result);
    }

    [Fact]
    public void Select_PackagePreferredOverHotSpotAndCore()
    {
        var readings = new List<SensorReading>
        {
            Reading("CPU Core #1", SensorType.Cpu, 50f),
            Reading("CPU Hot Spot", SensorType.Cpu, 55f),
            Reading("CPU Package", SensorType.Cpu, 48f),
        };

        var result = PrimaryTemperatureSelector.Select(readings, SensorType.Cpu);

        Assert.Equal(48f, result);
    }

    [Fact]
    public void Select_IgnoresDistanceToTjMaxForCoreFallback()
    {
        var readings = new List<SensorReading>
        {
            Reading("CPU Core #1 Distance to TjMax", SensorType.Cpu, 10f),
            Reading("CPU Core #1", SensorType.Cpu, 65f),
        };

        var result = PrimaryTemperatureSelector.Select(readings, SensorType.Cpu);

        Assert.Equal(65f, result);
    }

    [Fact]
    public void Select_EmptyReadings_ReturnsNull()
    {
        Assert.Null(PrimaryTemperatureSelector.Select([], SensorType.Cpu));
    }

    [Fact]
    public void SelectCoreAverage_ReturnsCoreAverageSensor()
    {
        var readings = new List<SensorReading>
        {
            Reading("CPU Core #1", SensorType.Cpu, 50f),
            Reading("CPU Core Average", SensorType.Cpu, 47f),
            Reading("CPU Package", SensorType.Cpu, 55f),
        };

        var result = PrimaryTemperatureSelector.SelectCoreAverage(readings, SensorType.Cpu);

        Assert.Equal(47f, result);
    }

    [Fact]
    public void SelectCoreAverage_NoCoreAverage_ReturnsNull()
    {
        var readings = new List<SensorReading>
        {
            Reading("CPU Package", SensorType.Cpu, 55f),
            Reading("CPU Core #1", SensorType.Cpu, 50f),
        };

        Assert.Null(PrimaryTemperatureSelector.SelectCoreAverage(readings, SensorType.Cpu));
    }

    [Fact]
    public void SelectCoreAverage_IgnoresOtherTypes()
    {
        var readings = new List<SensorReading>
        {
            Reading("GPU Core Average", SensorType.Gpu, 60f),
        };

        Assert.Null(PrimaryTemperatureSelector.SelectCoreAverage(readings, SensorType.Cpu));
    }
}