using HardwareTempWidget.Core;

namespace HardwareTempWidget.App;

public static class Localization
{
    private static readonly Dictionary<string, (string En, string Ru)> Strings = new()
    {
        ["Settings.Title"] = ("Settings — HardwareTempWidget", "Настройки — HardwareTempWidget"),
        ["Settings.Language"] = ("Language", "Язык"),
        ["Settings.Opacity"] = ("Opacity", "Прозрачность"),
        ["Settings.PollInterval"] = ("Poll interval (ms)", "Интервал опроса (мс)"),
        ["Settings.Autostart"] = ("Start with Windows", "Запускать вместе с Windows"),
        ["Settings.DisplayOnPanel"] = ("Show on panel", "Отображать на панели"),
        ["Settings.CpuMode"] = ("CPU display mode", "Режим отображения CPU"),
        ["Settings.CpuModeSmoothing"] = ("Smoothing", "Сглаживание"),
        ["Settings.CpuModeCoreAverage"] = ("Core average", "Среднее по ядрам"),
        ["Settings.CpuModeDefault"] = ("Default (package)", "Обычный (package)"),
        ["Settings.PanelValidation"] = (
            "Select at least one temperature to display on the panel.",
            "Выберите хотя бы одну температуру для отображения на панели."),
        ["Settings.TrayMetric"] = ("Temperature shown on tray icon", "Температура на значке в трее"),
        ["Settings.OverheatEnable"] = ("Notify on overheating", "Уведомлять о перегреве"),
        ["Settings.OverheatThreshold"] = ("Overheat threshold (°C)", "Порог перегрева (°C)"),
        ["Common.Cancel"] = ("Cancel", "Отмена"),
        ["Common.Save"] = ("Save", "Сохранить"),
        ["Menu.Settings"] = ("Settings…", "Настройки…"),
        ["Menu.AutostartOn"] = ("Autostart: on", "Автозапуск: включён"),
        ["Menu.AutostartOff"] = ("Autostart: off", "Автозапуск: выключен"),
        ["Menu.Exit"] = ("Exit", "Выход"),
        ["Tray.ToggleVisibility"] = ("Show/hide", "Показать/скрыть"),
        ["Notify.OverheatTitle"] = ("Overheating: {0}", "Перегрев {0}"),
        ["Notify.OverheatMessage"] = (
            "{0} temperature reached {1:F0}°C (threshold {2}°C).",
            "Температура {0} достигла {1:F0}°C (порог {2}°C)."),
        ["Tooltip.NoPerCoreData"] = ("Per-core data unavailable", "Данные по ядрам недоступны"),
        ["Settings.PerCoreUnavailable"] = (
            "Per-core CPU temperatures aren't available. Install a driver to enable them.",
            "Температура по отдельным ядрам CPU недоступна. Установите драйвер, чтобы это исправить."),
        ["Settings.PerCoreDriverInstalledButUnavailable"] = (
            "Driver is installed, but per-core data still isn't available. The app must be run as Administrator.",
            "Драйвер установлен, но данные по ядрам всё ещё недоступны. Приложение должно запускаться только от имени Администратора."),
        ["Settings.InstallDriver"] = ("Install driver", "Установить драйвер"),
        ["Settings.InstallingDriver"] = ("Installing…", "Установка…"),
        ["Settings.InstallSuccessRestarting"] = ("Installed. Restarting…", "Установлено. Перезапуск…"),
        ["Settings.InstallFailed"] = (
            "Installation failed. Try running as administrator, or install PawnIO manually from pawnio.eu.",
            "Не удалось установить. Попробуйте запустить от имени администратора или установите PawnIO вручную с pawnio.eu."),
    };

    public static AppLanguage Current { get; private set; } = AppLanguage.English;

    public static event Action? LanguageChanged;

    public static void Initialize(AppLanguage language) => Current = language;

    public static void SetLanguage(AppLanguage language)
    {
        if (Current == language)
        {
            return;
        }

        Current = language;
        LanguageChanged?.Invoke();
    }

    public static string T(string key)
    {
        if (!Strings.TryGetValue(key, out var value))
        {
            return key;
        }

        return Current == AppLanguage.Russian ? value.Ru : value.En;
    }
}
