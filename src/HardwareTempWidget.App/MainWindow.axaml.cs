using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using HardwareTempWidget.Core;

namespace HardwareTempWidget.App;

public partial class MainWindow : Window
{
    private readonly ISensorProvider _sensorProvider;
    private readonly SensorPollingService _pollingService;
    private readonly IAutostartService _autostartService;
    private readonly AppSettings _settings;

    public MainWindow()
    {
        InitializeComponent();

        _settings = SettingsStore.Load();
        _sensorProvider = SensorProviderFactory.Create();
        _autostartService = AutostartServiceFactory.Create();

        Opacity = _settings.Opacity;

        _pollingService = new SensorPollingService(_sensorProvider, TimeSpan.FromMilliseconds(_settings.PollIntervalMs));
        _pollingService.ReadingsUpdated += OnReadingsUpdated;

        Opened += OnOpened;
        Closing += OnClosing;
    }

    public AppSettings Settings => _settings;

    public SensorPollingService PollingService => _pollingService;

    public IAutostartService AutostartService => _autostartService;

    public void RefreshAutostartMenuHeader() =>
        AutostartMenuItem.Header = _autostartService.IsEnabled ? "Автозапуск: включён" : "Автозапуск: выключен";

    private void OnOpened(object? sender, EventArgs e)
    {
        var targetX = _settings.WindowLeft;
        var targetY = _settings.WindowTop;

        if (Screens.Primary is { } screen)
        {
            var area = screen.WorkingArea;

            targetX ??= area.Right - Width - 16;
            targetY ??= area.Bottom - Height - 16;

            targetX = Math.Clamp(targetX.Value, area.X, Math.Max(area.X, area.Right - Width));
            targetY = Math.Clamp(targetY.Value, area.Y, Math.Max(area.Y, area.Bottom - Height));
        }

        if (targetX is { } x && targetY is { } y)
        {
            Position = new PixelPoint((int)x, (int)y);
        }

        RefreshAutostartMenuHeader();
        _pollingService.Start();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        _settings.WindowLeft = Position.X;
        _settings.WindowTop = Position.Y;
        SettingsStore.Save(_settings);
        _pollingService.Dispose();
    }

    private void OnReadingsUpdated(object? sender, IReadOnlyList<SensorReading> readings)
    {
        var cpu = PrimaryTemperatureSelector.Select(readings, SensorType.Cpu);
        var gpu = PrimaryTemperatureSelector.Select(readings, SensorType.Gpu);

        Dispatcher.UIThread.Post(() =>
        {
            CpuValueText.Text = FormatTemperature(cpu);
            CpuValueText.Foreground = ColorFor(cpu);
            GpuValueText.Text = FormatTemperature(gpu);
            GpuValueText.Foreground = ColorFor(gpu);
        });
    }

    private static string FormatTemperature(float? celsius) => celsius is { } value ? $"{value:F0}°C" : "N/A";

    private static IBrush ColorFor(float? celsius) => celsius switch
    {
        null => Brushes.Gray,
        <= 60 => Brushes.LimeGreen,
        <= 80 => Brushes.Orange,
        _ => Brushes.OrangeRed,
    };

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(this);
        settingsWindow.Show(this);
    }

    private void OnToggleAutostartClick(object? sender, RoutedEventArgs e)
    {
        if (_autostartService.IsEnabled)
        {
            _autostartService.Disable();
        }
        else
        {
            _autostartService.Enable();
        }

        RefreshAutostartMenuHeader();
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }

    public void ToggleVisibility()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }
}
