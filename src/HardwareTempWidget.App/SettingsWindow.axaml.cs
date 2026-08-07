using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media;
using HardwareTempWidget.Core;
using HardwareTempWidget.Sensors.Windows;

namespace HardwareTempWidget.App;

public partial class SettingsWindow : Window
{
    private readonly MainWindow _mainWindow;
    private readonly IAppUpdater _updater;
    private AppUpdateInfo? _update;

    public SettingsWindow(MainWindow mainWindow)
    {
        InitializeComponent();

        _mainWindow = mainWindow;
        _updater = new AppUpdater();

        _mainWindow.TemperaturesChanged += OnMainTemperaturesChanged;
        Closing += (_, _) => _mainWindow.TemperaturesChanged -= OnMainTemperaturesChanged;
        UpdateMetricIcon();

        Opened += (_, _) => RemoveResizeButtons();

        ApplyLocalization();
        PopulateCpuModeComboBox();
        RefreshPerCoreSection();
        UpdateVersionText.Text = string.Format(Localization.T("Update.CurrentVersion"), VersionHelper.Current.ToString(3));
        _ = CheckForUpdatesAsync();

        var settings = mainWindow.Settings;
        LanguageComboBox.SelectedIndex = settings.Language == AppLanguage.Russian ? 1 : 0;

        OpacitySlider.Value = settings.Opacity;
        OpacitySlider.ValueChanged += OnOpacityChanged;
        UpdateOpacityPreview(settings.Opacity);
        ColorWheelControl.ColorChanged += OnWheelColorChanged;
        BuildPresetSwatches();
        UpdateBackgroundPreview(Color.TryParse(settings.PanelBackgroundColor, out var initColor)
            ? initColor
            : Color.Parse("#CC1E1E28"));
        FontSizeSlider.Value = settings.PanelFontSize;
        FontSizeSlider.ValueChanged += OnFontSizeChanged;
        UpdateFontPreview(settings.PanelFontSize);
        IntervalUpDown.Value = settings.PollIntervalMs;
        AutostartCheckBox.IsChecked = mainWindow.AutostartService.IsEnabled;
        ShowCpuOnPanelCheckBox.IsChecked = settings.ShowCpuOnPanel;
        ShowGpuOnPanelCheckBox.IsChecked = settings.ShowGpuOnPanel;
        ShowCpuOnPanelCheckBox.IsCheckedChanged += OnPanelSelectionChanged;
        ShowGpuOnPanelCheckBox.IsCheckedChanged += OnPanelSelectionChanged;
        UpdatePanelVisibilityPreview();

        CpuModeComboBox.SelectedIndex = (int)settings.CpuDisplayMode;

        TrayCpuRadioButton.IsChecked = settings.TrayIconMetric == SensorType.Cpu;
        TrayGpuRadioButton.IsChecked = settings.TrayIconMetric == SensorType.Gpu;

        OverheatEnabledCheckBox.IsChecked = settings.OverheatNotificationsEnabled;
        OverheatThresholdUpDown.Value = settings.OverheatThresholdCelsius;
    }

    private void ApplyLocalization()
    {
        Title = Localization.T("Settings.Title");
        GeneralTab.Header = Localization.T("Tab.General");
        AppearanceTab.Header = Localization.T("Tab.Appearance");
        PanelTab.Header = Localization.T("Tab.Panel");
        NotificationsTab.Header = Localization.T("Tab.Notifications");
        LanguageLabel.Text = Localization.T("Settings.Language");
        OpacityLabel.Text = Localization.T("Settings.Opacity");
        BackgroundColorLabel.Text = Localization.T("Settings.BackgroundColor");
        FontSizeLabel.Text = Localization.T("Settings.FontSize");
        PollIntervalLabel.Text = Localization.T("Settings.PollInterval");
        AutostartCheckBox.Content = Localization.T("Settings.Autostart");
        DisplayOnPanelLabel.Text = Localization.T("Settings.DisplayOnPanel");
        CpuModeLabel.Text = Localization.T("Settings.CpuMode");
        PanelValidationText.Text = Localization.T("Settings.PanelValidation");
        TrayMetricLabel.Text = Localization.T("Settings.TrayMetric");
        OverheatEnabledCheckBox.Content = Localization.T("Settings.OverheatEnable");
        OverheatThresholdLabel.Text = Localization.T("Settings.OverheatThreshold");
        CancelButton.Content = Localization.T("Common.Cancel");
        SaveButton.Content = Localization.T("Common.Save");
        UpdatesTab.Header = Localization.T("Tab.Updates");
        UpdateCheckButton.Content = Localization.T("Update.CheckNow");
        UpdateInstallButton.Content = Localization.T("Update.DownloadInstall");
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (ShowCpuOnPanelCheckBox.IsChecked != true && ShowGpuOnPanelCheckBox.IsChecked != true)
        {
            PanelValidationText.IsVisible = true;
            return;
        }

        PanelValidationText.IsVisible = false;

        var settings = _mainWindow.Settings;
        settings.Language = LanguageComboBox.SelectedIndex == 1 ? AppLanguage.Russian : AppLanguage.English;
        settings.Opacity = OpacitySlider.Value;
        settings.PanelBackgroundColor = _hexColor;
        settings.PanelFontSize = FontSizeSlider.Value;
        settings.PollIntervalMs = (int)(IntervalUpDown.Value ?? settings.PollIntervalMs);

        settings.ShowCpuOnPanel = ShowCpuOnPanelCheckBox.IsChecked == true;
        settings.ShowGpuOnPanel = ShowGpuOnPanelCheckBox.IsChecked == true;

        settings.TrayIconMetric = TrayGpuRadioButton.IsChecked == true ? SensorType.Gpu : SensorType.Cpu;

        settings.CpuDisplayMode = (CpuDisplayMode)CpuModeComboBox.SelectedIndex;

        settings.OverheatNotificationsEnabled = OverheatEnabledCheckBox.IsChecked == true;
        settings.OverheatThresholdCelsius = (int)(OverheatThresholdUpDown.Value ?? settings.OverheatThresholdCelsius);

        _mainWindow.ApplyPanelBackground();
        _mainWindow.ApplyPanelFontSize();
        _mainWindow.PollingService.Interval = TimeSpan.FromMilliseconds(settings.PollIntervalMs);
        _mainWindow.ApplyPanelVisibility();

        if (AutostartCheckBox.IsChecked == true)
        {
            _mainWindow.AutostartService.Enable();
        }
        else
        {
            _mainWindow.AutostartService.Disable();
        }

        SettingsStore.Save(settings);
        Localization.SetLanguage(settings.Language);
        _mainWindow.RefreshAutostartMenuHeader();
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

    private void OnMainTemperaturesChanged(float? cpu, float? gpu)
    {
        var metric = _mainWindow.Settings.TrayIconMetric == SensorType.Cpu ? cpu : gpu;
        Icon = TrayIconRenderer.Render(metric);
    }

    private void UpdateMetricIcon()
    {
        var metric = PrimaryTemperatureSelector.Select(_mainWindow.LastReadings, _mainWindow.Settings.TrayIconMetric);
        Icon = TrayIconRenderer.Render(metric);
    }

    private const int GWL_STYLE = -16;
    private const int WS_MINIMIZEBOX = 0x00020000;
    private const int WS_MAXIMIZEBOX = 0x00010000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern nint GetWindowLongPtr64(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern nint SetWindowLongPtr64(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    private static int GetWindowLong(nint hWnd, int nIndex) =>
        nint.Size == 8 ? (int)GetWindowLongPtr64(hWnd, nIndex) : GetWindowLong32(hWnd, nIndex);

    private static void SetWindowLong(nint hWnd, int nIndex, int dwNewLong)
    {
        if (nint.Size == 8)
        {
            _ = SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        }
        else
        {
            SetWindowLong(hWnd, nIndex, dwNewLong);
        }
    }

    private void RemoveResizeButtons()
    {
        if (TryGetPlatformHandle()?.Handle is not nint hWnd || hWnd == nint.Zero)
        {
            return;
        }

        var style = GetWindowLong(hWnd, GWL_STYLE) & ~WS_MINIMIZEBOX & ~WS_MAXIMIZEBOX;
        SetWindowLong(hWnd, GWL_STYLE, style);
    }

    private void OnWheelColorChanged(object? sender, Color e)
    {
        var a = Color.Parse(_hexColor).A;
        UpdateBackgroundPreview(new Color(a, e.R, e.G, e.B));
    }

    private void BuildPresetSwatches()
    {
        var presets = new[]
        {
            "#FF1E1E28", "#FF0F1115", "#FF808080",
            "#FF0000FF", "#FFFFFF00",
            "#FFFF00FF", "#FF00FFFF", "#FFFFA500", "#FFFFC0CB",
            "#FFA52A2A", "#FF000080", "#FF800000",
        };

        foreach (var hex in presets)
        {
            var border = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new Avalonia.CornerRadius(3),
                Margin = new Avalonia.Thickness(0, 0, 6, 6),
                Background = new SolidColorBrush(Color.Parse(hex)),
                Tag = hex,
            };
            border.PointerPressed += (_, _) => UpdateBackgroundPreview(Color.Parse((string)border.Tag!));
            PresetColorsPanel.Children.Add(border);
        }
    }

    private void UpdateBackgroundPreview(Color color)
    {
        _hexColor = color.ToString();
        PreviewBorder.Background = new SolidColorBrush(ApplyOpacity(color, OpacitySlider.Value));
        ColorSwatch.Background = new SolidColorBrush(color);
        ColorValueText.Text = _hexColor;
        ColorWheelControl.Color = new Color(255, color.R, color.G, color.B);
    }

    private string _hexColor = "#CC1E1E28";

    private static Color ApplyOpacity(Color color, double opacity) =>
        new((byte)Math.Clamp(color.A * opacity, 0, 255), color.R, color.G, color.B);

    private void OnOpacityChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        UpdateOpacityPreview(e.NewValue);
    }

    private void UpdateOpacityPreview(double opacity)
    {
        var color = Color.Parse(_hexColor);
        PreviewBorder.Background = new SolidColorBrush(ApplyOpacity(color, opacity));
        OpacityValueText.Text = opacity.ToString("0%");
    }

    private void OnFontSizeChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        UpdateFontPreview(e.NewValue);
    }

    private void OnPanelSelectionChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdatePanelVisibilityPreview();
        _mainWindow.SetPanelVisibility(
            ShowCpuOnPanelCheckBox.IsChecked == true,
            ShowGpuOnPanelCheckBox.IsChecked == true);
    }

    private void UpdateFontPreview(double size)
    {
        FontSizeValueText.Text = size.ToString("F0");
        PreviewCpuLabel.FontSize = size * 0.8;
        PreviewGpuLabel.FontSize = size * 0.8;
        PreviewCpuValue.FontSize = size;
        PreviewGpuValue.FontSize = size;
    }

    private void UpdatePanelVisibilityPreview()
    {
        PreviewCpuPanel.IsVisible = ShowCpuOnPanelCheckBox.IsChecked == true;
        PreviewGpuPanel.IsVisible = ShowGpuOnPanelCheckBox.IsChecked == true;
    }

    private async void OnUpdateCheckClick(object? sender, RoutedEventArgs e) => await CheckForUpdatesAsync();

    private async Task CheckForUpdatesAsync()
    {
        UpdateCheckButton.IsEnabled = false;
        UpdateInstallButton.IsVisible = false;
        UpdateProgressBar.IsVisible = false;
        UpdateStatusText.Text = Localization.T("Update.Checking");

        var update = await _updater.CheckForUpdateAsync(VersionHelper.Current);

        UpdateCheckButton.IsEnabled = true;
        if (update is null)
        {
            UpdateStatusText.Text = Localization.T("Update.UpToDate");
            return;
        }

        _update = update;
        UpdateStatusText.Text = string.Format(Localization.T("Update.Available"), update.Version.ToString(3));
        UpdateInstallButton.IsVisible = true;
    }

    private async void OnUpdateInstallClick(object? sender, RoutedEventArgs e)
    {
        if (_update is null)
        {
            return;
        }

        UpdateCheckButton.IsEnabled = false;
        UpdateInstallButton.IsEnabled = false;
        UpdateProgressBar.IsVisible = true;
        UpdateStatusText.Text = Localization.T("Update.Downloading");

        await _updater.DownloadAndInstallAsync(_update, value =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateProgressBar.Value = value * 100;
                UpdateStatusText.Text = $"{Localization.T("Update.Downloading")} {value * 100:F0}%";
            });
        });

        UpdateStatusText.Text = Localization.T("Update.Installed");
        await Task.Delay(500);
        RestartApp();
    }

    private void OnCheckUpdatesClick(object? sender, RoutedEventArgs e)
    {
        var updateWindow = new UpdateWindow();
        updateWindow.Show(this);
    }

    private void PopulateCpuModeComboBox()
    {
        CpuModeComboBox.Items.Clear();
        CpuModeComboBox.Items.Add(Localization.T("Settings.CpuModeSmoothing"));
        CpuModeComboBox.Items.Add(Localization.T("Settings.CpuModeCoreAverage"));
        CpuModeComboBox.Items.Add(Localization.T("Settings.CpuModeDefault"));
    }

    private void RefreshPerCoreSection()
    {
        if (CpuCoreReadings.Extract(_mainWindow.LastReadings).Count > 0)
        {
            PerCorePanel.IsVisible = false;
            return;
        }

        PerCorePanel.IsVisible = true;

        if (PawnIoInstaller.IsInstalled())
        {
            PerCoreStatusText.Text = Localization.T("Settings.PerCoreDriverInstalledButUnavailable");
            InstallDriverButton.IsVisible = false;
        }
        else
        {
            PerCoreStatusText.Text = Localization.T("Settings.PerCoreUnavailable");
            InstallDriverButton.IsVisible = true;
            InstallDriverButton.IsEnabled = true;
            InstallDriverButton.Content = Localization.T("Settings.InstallDriver");
        }
    }

    private async void OnInstallDriverClick(object? sender, RoutedEventArgs e)
    {
        InstallDriverButton.IsEnabled = false;
        InstallDriverButton.Content = Localization.T("Settings.InstallingDriver");

        var success = await PawnIoInstaller.InstallAsync();

        if (!success)
        {
            PerCoreStatusText.Text = Localization.T("Settings.InstallFailed");
            InstallDriverButton.IsEnabled = true;
            InstallDriverButton.Content = Localization.T("Settings.InstallDriver");
            return;
        }

        PerCoreStatusText.Text = Localization.T("Settings.InstallSuccessRestarting");
        InstallDriverButton.IsVisible = false;
        await Task.Delay(1000);
        RestartApp();
    }

    private static void RestartApp()
    {
        if (Environment.ProcessPath is { } exePath)
        {
            Process.Start(exePath);
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
        else
        {
            Environment.Exit(0);
        }
    }
}
