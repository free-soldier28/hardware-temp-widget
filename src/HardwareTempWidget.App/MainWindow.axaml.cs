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
    private const double FallbackTaskbarHeight = 40;

    private readonly ISensorProvider _sensorProvider;
    private readonly SensorPollingService _pollingService;
    private readonly IAutostartService _autostartService;
    private readonly IOverheatNotifier _overheatNotifier;
    private readonly AppSettings _settings;

    private bool _cpuOverheating;
    private bool _gpuOverheating;

    public MainWindow()
    {
        InitializeComponent();

        _settings = SettingsStore.Load();
        _sensorProvider = SensorProviderFactory.Create();
        _autostartService = AutostartServiceFactory.Create();
        _overheatNotifier = OverheatNotifierFactory.Create();

        Opacity = _settings.Opacity;
        ApplyPanelVisibility();

        _pollingService = new SensorPollingService(_sensorProvider, TimeSpan.FromMilliseconds(_settings.PollIntervalMs));
        _pollingService.ReadingsUpdated += OnReadingsUpdated;

        ApplyLocalization();
        Localization.LanguageChanged += ApplyLocalization;

        Opened += OnOpened;
        Closing += OnClosing;
    }

    public event Action<float?, float?>? TemperaturesChanged;

    public IReadOnlyList<SensorReading> LastReadings { get; private set; } = Array.Empty<SensorReading>();

    public AppSettings Settings => _settings;

    public SensorPollingService PollingService => _pollingService;

    public IAutostartService AutostartService => _autostartService;

    public void RefreshAutostartMenuHeader() =>
        AutostartMenuItem.Header = _autostartService.IsEnabled
            ? Localization.T("Menu.AutostartOn")
            : Localization.T("Menu.AutostartOff");

    private void ApplyLocalization()
    {
        SettingsMenuItem.Header = Localization.T("Menu.Settings");
        ExitMenuItem.Header = Localization.T("Menu.Exit");
        RefreshAutostartMenuHeader();
    }

    public void ApplyPanelVisibility()
    {
        CpuPanel.IsVisible = _settings.ShowCpuOnPanel;
        GpuPanel.IsVisible = _settings.ShowGpuOnPanel;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        // Positioning uses raw Win32 pixel coordinates throughout (matching what
        // SetWindowPos ultimately expects) rather than Avalonia's Screens API,
        // whose reported WorkingArea does not line up with Win32 in this host.
        var taskbar = TaskbarInfo.GetTaskbarBounds();
        var trayNotify = TaskbarInfo.GetTrayNotifyBounds();
        var widthPx = (int)(Width * RenderScaling);

        Height = taskbar is { } tb ? tb.Height / RenderScaling : FallbackTaskbarHeight;
        var heightPx = (int)(Height * RenderScaling);

        if (_settings.WindowLeft is { } savedX && _settings.WindowTop is { } savedY)
        {
            Position = new PixelPoint((int)savedX, (int)savedY);
        }
        else if (trayNotify is { } tray && taskbar is { } dockedTaskbar)
        {
            Position = new PixelPoint(tray.X - widthPx, dockedTaskbar.Y);
        }
        else
        {
            var area = TaskbarInfo.GetPrimaryWorkArea();
            Position = new PixelPoint(area.Right - widthPx - 16, area.Bottom - heightPx - 16);
        }

        RefreshAutostartMenuHeader();
        ForceTopmost();
        _pollingService.Start();
    }

    private void ForceTopmost()
    {
        var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        TaskbarInfo.ForceTopmost(handle);
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
        LastReadings = readings;

        var cpu = PrimaryTemperatureSelector.Select(readings, SensorType.Cpu);
        var gpu = PrimaryTemperatureSelector.Select(readings, SensorType.Gpu);

        CheckOverheat(SensorType.Cpu, cpu, ref _cpuOverheating);
        CheckOverheat(SensorType.Gpu, gpu, ref _gpuOverheating);

        var cpuCoreTooltip = BuildCpuCoreTooltip(readings);

        Dispatcher.UIThread.Post(() =>
        {
            CpuValueText.Text = FormatTemperature(cpu);
            CpuValueText.Foreground = ColorFor(cpu);
            GpuValueText.Text = FormatTemperature(gpu);
            GpuValueText.Foreground = ColorFor(gpu);
            ToolTip.SetTip(CpuPanel, cpuCoreTooltip);

            ForceTopmost();
            TemperaturesChanged?.Invoke(cpu, gpu);
        });
    }

    private static string BuildCpuCoreTooltip(IReadOnlyList<SensorReading> readings)
    {
        var cores = CpuCoreReadings.Extract(readings);

        if (cores.Count == 0)
        {
            return Localization.T("Tooltip.NoPerCoreData");
        }

        return string.Join(Environment.NewLine,
            cores.Select(c => $"{c.Name}: {c.TemperatureCelsius:F0}°C"));
    }

    private void CheckOverheat(SensorType type, float? celsius, ref bool isOverheating)
    {
        if (!_settings.OverheatNotificationsEnabled || celsius is not { } value)
        {
            return;
        }

        var threshold = _settings.OverheatThresholdCelsius;

        if (value >= threshold && !isOverheating)
        {
            isOverheating = true;
            var label = type == SensorType.Cpu ? "CPU" : "GPU";
            _overheatNotifier.Notify(
                string.Format(Localization.T("Notify.OverheatTitle"), label),
                string.Format(Localization.T("Notify.OverheatMessage"), label, value, threshold));
        }
        else if (value < threshold - 3)
        {
            isOverheating = false;
        }
    }

    private static string FormatTemperature(float? celsius) => celsius is { } value ? $"{value:F0}°" : "--°";

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

    private void OnSettingsClick(object? sender, RoutedEventArgs e) => ShowSettings();

    public void ShowSettings()
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
