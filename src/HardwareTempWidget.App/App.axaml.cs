using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace HardwareTempWidget.App;

public partial class App : Application
{
    private MainWindow? _mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _mainWindow = new MainWindow();
            _mainWindow.TemperaturesChanged += OnTemperaturesChanged;
            desktop.MainWindow = _mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnTemperaturesChanged(float? cpu, float? gpu)
    {
        if (_mainWindow is not { } mainWindow || TrayIcon.GetIcons(this) is not { Count: > 0 } icons)
        {
            return;
        }

        var metricValue = mainWindow.Settings.TrayIconMetric == HardwareTempWidget.Core.SensorType.Cpu ? cpu : gpu;
        icons[0].Icon = TrayIconRenderer.Render(metricValue);
        icons[0].ToolTipText = $"CPU: {Format(cpu)}   GPU: {Format(gpu)}";
    }

    private static string Format(float? celsius) => celsius is { } value ? $"{value:F0}°C" : "N/A";

    private void OnTrayIconClicked(object? sender, EventArgs e) => _mainWindow?.ToggleVisibility();

    private void OnTrayToggleClick(object? sender, EventArgs e) => _mainWindow?.ToggleVisibility();

    private void OnTraySettingsClick(object? sender, EventArgs e) => _mainWindow?.ShowSettings();

    private void OnTrayExitClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }
}
