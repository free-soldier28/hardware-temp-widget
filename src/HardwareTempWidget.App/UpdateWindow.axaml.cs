using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using HardwareTempWidget.Core;

namespace HardwareTempWidget.App;

public partial class UpdateWindow : Window
{
    private readonly IAppUpdater _updater;
    private AppUpdateInfo? _update;

    public UpdateWindow()
        : this(new AppUpdater())
    {
    }

    public UpdateWindow(IAppUpdater updater)
    {
        InitializeComponent();
        _updater = updater;

        ApplyLocalization();
        VersionText.Text = string.Format(Localization.T("Update.CurrentVersion"), VersionHelper.Current.ToString(3));
        _ = CheckForUpdatesAsync();
    }

    private void ApplyLocalization()
    {
        Title = Localization.T("Update.Title");
        CheckButton.Content = Localization.T("Update.CheckNow");
        InstallButton.Content = Localization.T("Update.DownloadInstall");
        CloseButton.Content = Localization.T("Common.Close");
    }

    private async Task CheckForUpdatesAsync()
    {
        SetBusy(true);
        StatusText.Text = Localization.T("Update.Checking");
        InstallButton.IsVisible = false;

        var update = await _updater.CheckForUpdateAsync(VersionHelper.Current);

        SetBusy(false);
        if (update is null)
        {
            StatusText.Text = Localization.T("Update.UpToDate");
            return;
        }

        _update = update;
        StatusText.Text = string.Format(Localization.T("Update.Available"), update.Version.ToString(3));
        InstallButton.IsVisible = true;
    }

    private async void OnCheckClick(object? sender, RoutedEventArgs e) => await CheckForUpdatesAsync();

    private async void OnInstallClick(object? sender, RoutedEventArgs e)
    {
        if (_update is null)
        {
            return;
        }

        SetBusy(true);
        ProgressBar.IsVisible = true;
        StatusText.Text = Localization.T("Update.Downloading");

        await _updater.DownloadAndInstallAsync(_update, value =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ProgressBar.Value = value * 100;
                StatusText.Text = $"{Localization.T("Update.Downloading")} {value * 100:F0}%";
            });
        });

        StatusText.Text = Localization.T("Update.Installed");
        await Task.Delay(500);
        RestartApp();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void SetBusy(bool busy)
    {
        CheckButton.IsEnabled = !busy;
        InstallButton.IsEnabled = !busy;
    }

    private static void RestartApp()
    {
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