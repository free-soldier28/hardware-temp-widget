using System.Text.Json;

namespace HardwareTempWidget.Core;

public static class SettingsStore
{
    private static readonly string DefaultDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HardwareTempWidget");

    /// <summary>
    /// Resolves the default settings directory. Overridable by the test suite so the
    /// parameterless API can be exercised without touching the real AppData folder.
    /// </summary>
    internal static Func<string> DefaultDirectoryResolver { get; set; } = () => DefaultDirectory;

    public static string FilePath => Path.Combine(DefaultDirectoryResolver(), "settings.json");

    public static AppSettings Load() => Load(DefaultDirectoryResolver());

    public static void Save(AppSettings settings) => Save(DefaultDirectoryResolver(), settings);

    public static AppSettings Load(string directory)
    {
        try
        {
            var filePath = Path.Combine(directory, "settings.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings is not null)
                {
                    return settings;
                }
            }
        }
        catch
        {
            // Corrupt or unreadable settings file; fall back to defaults.
        }

        return new AppSettings();
    }

    public static void Save(string directory, AppSettings settings)
    {
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, "settings.json");
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }
}
