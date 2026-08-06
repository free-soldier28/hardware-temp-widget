namespace HardwareTempWidget.Core.Tests;

public class AppSettingsTests
{
    [Fact]
    public void Defaults_AreAsExpected()
    {
        var settings = new AppSettings();

        Assert.Equal(AppLanguage.English, settings.Language);
        Assert.Equal(0.85, settings.Opacity);
        Assert.Equal(1500, settings.PollIntervalMs);
        Assert.Null(settings.WindowLeft);
        Assert.Null(settings.WindowTop);
        Assert.True(settings.ShowCpuOnPanel);
        Assert.True(settings.ShowGpuOnPanel);
        Assert.Equal(SensorType.Cpu, settings.TrayIconMetric);
        Assert.Equal(CpuDisplayMode.Smoothing, settings.CpuDisplayMode);
        Assert.True(settings.OverheatNotificationsEnabled);
        Assert.Equal(85, settings.OverheatThresholdCelsius);
    }
}

public class SensorReadingTests
{
    [Fact]
    public void Constructor_StoresValues()
    {
        var reading = new SensorReading("CPU Package", SensorType.Cpu, 41.5f);

        Assert.Equal("CPU Package", reading.Name);
        Assert.Equal(SensorType.Cpu, reading.Type);
        Assert.Equal(41.5f, reading.TemperatureCelsius);
    }

    [Fact]
    public void Equality_ValuesBased()
    {
        var a = new SensorReading("CPU", SensorType.Cpu, 40f);
        var b = new SensorReading("CPU", SensorType.Cpu, 40f);
        var c = new SensorReading("GPU", SensorType.Gpu, 40f);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Deconstruct_ExposesComponents()
    {
        var (name, type, temp) = new SensorReading("GPU", SensorType.Gpu, 55f);

        Assert.Equal("GPU", name);
        Assert.Equal(SensorType.Gpu, type);
        Assert.Equal(55f, temp);
    }

    [Theory]
    [InlineData(SensorType.Cpu)]
    [InlineData(SensorType.Gpu)]
    public void SensorType_EnumVariants_Exist(SensorType type)
    {
        Assert.True(Enum.IsDefined(type));
    }
}

public class AppLanguageTests
{
    [Theory]
    [InlineData(AppLanguage.English)]
    [InlineData(AppLanguage.Russian)]
    public void Language_EnumVariants_Exist(AppLanguage language)
    {
        Assert.True(Enum.IsDefined(language));
    }
}