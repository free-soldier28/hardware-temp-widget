using Avalonia.Controls;
using Avalonia.Interactivity;
using HardwareTempWidget.Core;

namespace HardwareTempWidget.App;

public partial class SettingsWindow : Window
{
    private readonly MainWindow _mainWindow;

    public SettingsWindow(MainWindow mainWindow)
    {
        InitializeComponent();

        _mainWindow = mainWindow;

        var settings = mainWindow.Settings;
        OpacitySlider.Value = settings.Opacity;
        IntervalUpDown.Value = settings.PollIntervalMs;
        AutostartCheckBox.IsChecked = mainWindow.AutostartService.IsEnabled;

        ShowCpuOnPanelCheckBox.IsChecked = settings.ShowCpuOnPanel;
        ShowGpuOnPanelCheckBox.IsChecked = settings.ShowGpuOnPanel;

        TrayCpuRadioButton.IsChecked = settings.TrayIconMetric == SensorType.Cpu;
        TrayGpuRadioButton.IsChecked = settings.TrayIconMetric == SensorType.Gpu;

        OverheatEnabledCheckBox.IsChecked = settings.OverheatNotificationsEnabled;
        OverheatThresholdUpDown.Value = settings.OverheatThresholdCelsius;
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        var settings = _mainWindow.Settings;
        settings.Opacity = OpacitySlider.Value;
        settings.PollIntervalMs = (int)(IntervalUpDown.Value ?? settings.PollIntervalMs);

        settings.ShowCpuOnPanel = ShowCpuOnPanelCheckBox.IsChecked == true;
        settings.ShowGpuOnPanel = ShowGpuOnPanelCheckBox.IsChecked == true;
        if (!settings.ShowCpuOnPanel && !settings.ShowGpuOnPanel)
        {
            settings.ShowCpuOnPanel = true;
        }

        settings.TrayIconMetric = TrayGpuRadioButton.IsChecked == true ? SensorType.Gpu : SensorType.Cpu;

        settings.OverheatNotificationsEnabled = OverheatEnabledCheckBox.IsChecked == true;
        settings.OverheatThresholdCelsius = (int)(OverheatThresholdUpDown.Value ?? settings.OverheatThresholdCelsius);

        _mainWindow.Opacity = settings.Opacity;
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
        _mainWindow.RefreshAutostartMenuHeader();
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();
}
