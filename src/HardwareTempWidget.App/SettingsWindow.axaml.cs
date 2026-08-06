using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using HardwareTempWidget.Core;
using HardwareTempWidget.Sensors.Windows;

namespace HardwareTempWidget.App;

public partial class SettingsWindow : Window
{
    private readonly MainWindow _mainWindow;

    public SettingsWindow(MainWindow mainWindow)
    {
        InitializeComponent();

        _mainWindow = mainWindow;

        ApplyLocalization();
        PopulateCpuModeComboBox();
        RefreshPerCoreSection();

        var settings = mainWindow.Settings;
        LanguageComboBox.SelectedIndex = settings.Language == AppLanguage.Russian ? 1 : 0;

        OpacitySlider.Value = settings.Opacity;
        IntervalUpDown.Value = settings.PollIntervalMs;
        AutostartCheckBox.IsChecked = mainWindow.AutostartService.IsEnabled;

        ShowCpuOnPanelCheckBox.IsChecked = settings.ShowCpuOnPanel;
        ShowGpuOnPanelCheckBox.IsChecked = settings.ShowGpuOnPanel;

        CpuModeComboBox.SelectedIndex = (int)settings.CpuDisplayMode;

        TrayCpuRadioButton.IsChecked = settings.TrayIconMetric == SensorType.Cpu;
        TrayGpuRadioButton.IsChecked = settings.TrayIconMetric == SensorType.Gpu;

        OverheatEnabledCheckBox.IsChecked = settings.OverheatNotificationsEnabled;
        OverheatThresholdUpDown.Value = settings.OverheatThresholdCelsius;
    }

    private void ApplyLocalization()
    {
        Title = Localization.T("Settings.Title");
        LanguageLabel.Text = Localization.T("Settings.Language");
        OpacityLabel.Text = Localization.T("Settings.Opacity");
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
        settings.PollIntervalMs = (int)(IntervalUpDown.Value ?? settings.PollIntervalMs);

        settings.ShowCpuOnPanel = ShowCpuOnPanelCheckBox.IsChecked == true;
        settings.ShowGpuOnPanel = ShowGpuOnPanelCheckBox.IsChecked == true;

        settings.TrayIconMetric = TrayGpuRadioButton.IsChecked == true ? SensorType.Gpu : SensorType.Cpu;

        settings.CpuDisplayMode = (CpuDisplayMode)CpuModeComboBox.SelectedIndex;

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
        Localization.SetLanguage(settings.Language);
        _mainWindow.RefreshAutostartMenuHeader();
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

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
