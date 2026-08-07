namespace HardwareTempWidget.Core.Tests;

public class SettingsStoreTests : IDisposable
{
    private readonly string _directory;

    public SettingsStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "HardwareTempWidgetTests", Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var settings = SettingsStore.Load(_directory);

        Assert.NotNull(settings);
        Assert.Equal(AppLanguage.English, settings.Language);
        Assert.Equal(1.0, settings.Opacity);
        Assert.Equal(1500, settings.PollIntervalMs);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllValues()
    {
        var original = new AppSettings
        {
            Language = AppLanguage.Russian,
            Opacity = 0.5,
            PollIntervalMs = 2500,
            WindowLeft = 10.5,
            WindowTop = 20.75,
            ShowCpuOnPanel = false,
            ShowGpuOnPanel = true,
            TrayIconMetric = SensorType.Gpu,
            CpuDisplayMode = CpuDisplayMode.CoreAverage,
            OverheatNotificationsEnabled = false,
            OverheatThresholdCelsius = 90,
        };

        SettingsStore.Save(_directory, original);
        var loaded = SettingsStore.Load(_directory);

        Assert.Equal(original.Language, loaded.Language);
        Assert.Equal(original.Opacity, loaded.Opacity);
        Assert.Equal(original.PollIntervalMs, loaded.PollIntervalMs);
        Assert.Equal(original.WindowLeft, loaded.WindowLeft);
        Assert.Equal(original.WindowTop, loaded.WindowTop);
        Assert.Equal(original.ShowCpuOnPanel, loaded.ShowCpuOnPanel);
        Assert.Equal(original.ShowGpuOnPanel, loaded.ShowGpuOnPanel);
        Assert.Equal(original.TrayIconMetric, loaded.TrayIconMetric);
        Assert.Equal(original.CpuDisplayMode, loaded.CpuDisplayMode);
        Assert.Equal(original.OverheatNotificationsEnabled, loaded.OverheatNotificationsEnabled);
        Assert.Equal(original.OverheatThresholdCelsius, loaded.OverheatThresholdCelsius);
    }

    [Fact]
    public void Save_CreatesDirectoryWhenMissing()
    {
        var nested = Path.Combine(_directory, "a", "b", "c");
        SettingsStore.Save(nested, new AppSettings());

        Assert.True(File.Exists(Path.Combine(nested, "settings.json")));
    }

    [Fact]
    public void Load_CorruptJson_ReturnsDefaults()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "settings.json"), "{ not valid json !!");

        var settings = SettingsStore.Load(_directory);

        Assert.NotNull(settings);
        Assert.Equal(AppLanguage.English, settings.Language);
    }

    [Fact]
    public void Load_FileWithNullJson_ReturnsDefaults()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "settings.json"), "null");

        var settings = SettingsStore.Load(_directory);

        Assert.NotNull(settings);
        Assert.Equal(1500, settings.PollIntervalMs);
    }

    [Fact]
    public void Load_PartialJson_FillsMissingWithDefaults()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "settings.json"), "{\"PollIntervalMs\": 999}");

        var settings = SettingsStore.Load(_directory);

        Assert.Equal(999, settings.PollIntervalMs);
        Assert.Equal(AppLanguage.English, settings.Language);
    }

    [Fact]
    public void ParameterlessSaveAndLoad_RoundTrip_ThroughDefaultDirectoryResolver()
    {
        SettingsStore.DefaultDirectoryResolver = () => _directory;
        try
        {
            SettingsStore.Save(new AppSettings { Language = AppLanguage.Russian, PollIntervalMs = 3000 });
            var loaded = SettingsStore.Load();

            Assert.Equal(AppLanguage.Russian, loaded.Language);
            Assert.Equal(3000, loaded.PollIntervalMs);
        }
        finally
        {
            SettingsStore.DefaultDirectoryResolver = () => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HardwareTempWidget");
        }
    }

    [Fact]
    public void FilePath_EndsWithSettingsJson()
    {
        SettingsStore.DefaultDirectoryResolver = () => _directory;
        try
        {
            Assert.EndsWith("settings.json", SettingsStore.FilePath);
        }
        finally
        {
            SettingsStore.DefaultDirectoryResolver = () => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HardwareTempWidget");
        }
    }

    public void Dispose()
    {
        SettingsStore.DefaultDirectoryResolver = () => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HardwareTempWidget");

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}