# HardwareTempWidget

A lightweight Windows widget that shows live CPU and GPU temperature.
Docks to the taskbar right next to the system tray.

## Features

### Temperature display
- Live CPU and GPU temperature readings, refreshed at a configurable interval (1.5s by default).
- Color-coded values: green (≤60°C), orange (61–80°C), red (>80°C).
- Flexible display: CPU and GPU readings can be shown/hidden independently on the panel.
- Data source: [LibreHardwareMonitorLib](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor); GPU support (Nvidia/AMD/Intel) depends on whether the specific hardware/driver exposes a temperature sensor.

### Tray icon
- Renders the selected temperature (CPU or GPU — configurable) directly on the icon, no need to open the window.
- Tooltip shows both CPU and GPU values at once.
- Clicking the icon or the "Show/hide" menu item toggles the widget.

### Placement and look
- Borderless window, height matches the Windows taskbar exactly, docks to the left of the system tray icons by default.
- Can be dragged anywhere — position is remembered between launches.
- Semi-transparent background, always on top.

### Overheat notifications
- Configurable temperature threshold (°C) for CPU and GPU.
- A Windows system notification (Action Center) is sent when the threshold is crossed.
- Edge-triggered: fires once when the threshold is crossed (not on every poll), and can fire again only after cooling 3°C below the threshold and crossing it again.

### Settings (right-click → "Settings…")
- Window opacity.
- Sensor poll interval (ms).
- Launch with Windows (via the `HKCU\...\Run` registry key).
- Which temperatures to show on the widget panel (CPU / GPU).
- Which temperature drives the tray icon (CPU or GPU).
- Enable/disable overheat notifications and their threshold.

### Context menu (right-click the widget)
- Settings…
- Autostart (toggle with current-state indicator)
- Exit

## Architecture

The solution is designed with room for future non-Windows support:

```
HardwareTempWidget/
├── src/
│   ├── HardwareTempWidget.Core/            # platform-independent abstractions and models
│   │   ├── ISensorProvider.cs              # sensor reading interface
│   │   ├── IAutostartService.cs            # autostart management interface
│   │   ├── IOverheatNotifier.cs            # notification interface
│   │   ├── SensorReading.cs / SensorType.cs
│   │   ├── SensorPollingService.cs         # background sensor polling
│   │   ├── AppSettings.cs / SettingsStore.cs
│   ├── HardwareTempWidget.Sensors.Windows/ # Windows implementation
│   │   ├── WindowsSensorProvider.cs        # LibreHardwareMonitorLib
│   │   ├── WindowsAutostartService.cs      # Run registry key
│   │   └── WindowsToastNotifier.cs         # toast notifications (Microsoft.Toolkit.Uwp.Notifications)
│   └── HardwareTempWidget.App/             # Avalonia UI (Windows)
│       ├── MainWindow.axaml(.cs)           # taskbar-docked widget
│       ├── SettingsWindow.axaml(.cs)
│       ├── TaskbarInfo.cs                  # positioning relative to the taskbar/tray
│       ├── TrayIconRenderer.cs             # renders the temperature onto the tray icon
│       └── App.axaml(.cs)                  # entry point, tray menu
└── tests/
    └── HardwareTempWidget.Core.Tests/
```

Sensor reading, autostart, and notifications are hidden behind interfaces in `Core`, so
adding Linux/macOS support later only requires new implementations (e.g. `LinuxSensorProvider`)
without touching the UI.

## Requirements

- Windows 10 (1809+) / Windows 11
- .NET 8 SDK to build

## Build and run

```powershell
dotnet build
dotnet run --project src/HardwareTempWidget.App/HardwareTempWidget.App.csproj
```

## CI/CD

GitHub Actions (`.github/workflows/ci.yml`):
- Every push/PR: restore, build (Release), run `HardwareTempWidget.Core.Tests`.
- Every push to `main`: additionally publishes a self-contained win-x64 single-file build,
  uploads it as a workflow artifact, and updates the rolling `latest` GitHub Release with the
  zipped build.

Grab the newest build from the repo's [Releases](../../releases/tag/latest) page.

## Known limitations

- GPU temperature is only available if LibreHardwareMonitorLib exposes it for the specific
  GPU/driver — some integrated GPUs (e.g. certain Intel iGPUs) don't expose a temperature
  sensor at the driver level.
- Full access to some sensors (especially on certain motherboards) may require running as
  administrator.
