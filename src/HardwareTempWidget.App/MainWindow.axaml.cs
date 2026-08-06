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

    private readonly MovingAverage _cpuSmoother = new(5);

    private bool _positioned;

    public MainWindow()
    {
        InitializeComponent();

        _settings = SettingsStore.Load();
        _sensorProvider = SensorProviderFactory.Create();
        _autostartService = AutostartServiceFactory.Create();
        _overheatNotifier = OverheatNotifierFactory.Create();

        Opacity = _settings.Opacity;
        ApplyPanelBackground();
        ApplyPanelVisibility();
        ApplyPanelFontSize();

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

    public void ApplyPanelBackground()
    {
        if (Color.TryParse(_settings.PanelBackgroundColor, out var color))
        {
            Background = new SolidColorBrush(color);
        }
    }

    public void ApplyPanelVisibility()
    {
        CpuPanel.IsVisible = _settings.ShowCpuOnPanel;
        GpuPanel.IsVisible = _settings.ShowGpuOnPanel;
        if (_positioned)
        {
            ResizeToContent();
        }
    }

    public void ApplyPanelFontSize()
    {
        var size = _settings.PanelFontSize;
        CpuLabelText.FontSize = size * 0.8;
        GpuLabelText.FontSize = size * 0.8;
        CpuValueText.FontSize = size;
        GpuValueText.FontSize = size;
        if (_positioned)
        {
            ResizeToContent();
        }
    }

    public void ResizeToContent()
    {
        var oldWidthPx = (int)(Width * RenderScaling);
        var centerX = Position.X + oldWidthPx / 2;

        PanelHost.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Width = Math.Max(40, Math.Ceiling(PanelHost.DesiredSize.Width) + 8);

        var newWidthPx = (int)(Width * RenderScaling);
        Position = new PixelPoint(centerX - newWidthPx / 2, Position.Y);
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        PositionWindow();

        RefreshAutostartMenuHeader();
        ForceTopmost();
        _pollingService.Start();
    }

    private void PositionWindow()
    {
        // Positioning uses raw Win32 pixel coordinates throughout (matching what
        // SetWindowPos ultimately expects) rather than Avalonia's Screens API,
        // whose reported WorkingArea does not line up with Win32 in this host.
        ResizeToContent();

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

        _positioned = true;
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

        var cpu = SelectCpuTemperature(readings);
        var gpu = PrimaryTemperatureSelector.Select(readings, SensorType.Gpu);

        CheckOverheat(SensorType.Cpu, cpu, ref _cpuOverheating);
        CheckOverheat(SensorType.Gpu, gpu, ref _gpuOverheating);

        Dispatcher.UIThread.Post(() =>
        {
            CpuValueText.Text = FormatTemperature(cpu);
            CpuValueText.Foreground = ColorFor(cpu);
            GpuValueText.Text = FormatTemperature(gpu);
            GpuValueText.Foreground = ColorFor(gpu);
            ToolTip.SetTip(CpuPanel, BuildCpuCoreTooltip(readings));

            ForceTopmost();
            TemperaturesChanged?.Invoke(cpu, gpu);
        });
    }

    private float? SelectCpuTemperature(IReadOnlyList<SensorReading> readings)
    {
        var raw = PrimaryTemperatureSelector.Select(readings, SensorType.Cpu);

        return _settings.CpuDisplayMode switch
        {
            CpuDisplayMode.Smoothing => raw is { } value ? _cpuSmoother.Add(value) : null,
            CpuDisplayMode.CoreAverage =>
                PrimaryTemperatureSelector.SelectCoreAverage(readings, SensorType.Cpu) ?? raw,
            _ => raw,
        };
    }

    private static object BuildCpuCoreTooltip(IReadOnlyList<SensorReading> readings)
    {
        var groups = CpuCoreReadings.GroupByType(readings);

        if (groups.Count == 0)
        {
            return Localization.T("Tooltip.NoPerCoreData");
        }

        var tooltip = new StackPanel { Spacing = 6 };

        foreach (var (type, cores) in groups)
        {
            var rows = new StackPanel { Spacing = 2 };

            if (type.Length > 0)
            {
                rows.Children.Add(new TextBlock { Text = type, FontWeight = FontWeight.Bold });
            }

            foreach (var core in cores)
            {
                rows.Children.Add(new TextBlock
                {
                    Text = $"#{CpuCoreReadings.CoreNumber(core)}: {core.TemperatureCelsius:F0}°C",
                });
            }

            tooltip.Children.Add(new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 6),
                Child = rows,
            });
        }

        return tooltip;
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

    public void ShowUpdates()
    {
        var updateWindow = new UpdateWindow();
        updateWindow.Show(this);
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
