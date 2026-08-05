using HardwareTempWidget.Core;
using Microsoft.Toolkit.Uwp.Notifications;

namespace HardwareTempWidget.Sensors.Windows;

public sealed class WindowsToastNotifier : IOverheatNotifier
{
    public void Notify(string title, string message)
    {
        new ToastContentBuilder()
            .AddText(title)
            .AddText(message)
            .Show();
    }
}
