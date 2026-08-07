using HardwareTempWidget.Core;

namespace HardwareTempWidget.App;

public static class Localization
{
    private static readonly Dictionary<string, (string En, string Ru)> Strings = new()
    {
        ["Settings.Title"] = ("Settings — HardwareTempWidget", "Настройки — HardwareTempWidget"),
        ["Tab.General"] = ("General", "Общие"),
        ["Tab.Appearance"] = ("Appearance", "Внешний вид"),
        ["Tab.Panel"] = ("Panel & Tray", "Панель и трей"),
        ["Tab.Notifications"] = ("Notifications", "Уведомления"),
        ["Tab.Advanced"] = ("Advanced", "Дополнительно"),
        ["Tab.Updates"] = ("Updates", "Обновления"),
        ["Settings.Language"] = ("Language", "Язык"),
        ["Settings.Opacity"] = ("Opacity", "Прозрачность"),
        ["Settings.BackgroundColor"] = ("Background color", "Цвет фона"),
        ["Settings.PresetColors"] = ("Predefined dark colors", "Готовые тёмные цвета"),
        ["Settings.FontSize"] = ("Panel font size", "Размер шрифта на панели"),
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
        ["Common.Close"] = ("Close", "Закрыть"),
        ["Menu.Settings"] = ("Settings…", "Настройки…"),
        ["Menu.AutostartOn"] = ("Autostart: on", "Автозапуск: включён"),
        ["Menu.AutostartOff"] = ("Autostart: off", "Автозапуск: выключен"),
        ["Menu.CheckUpdates"] = ("Check for updates…", "Проверить обновления…"),
        ["Menu.Exit"] = ("Exit", "Выход"),
        ["Tray.ToggleVisibility"] = ("Show/hide", "Показать/скрыть"),
        ["Notify.OverheatTitle"] = ("Overheating: {0}", "Перегрев {0}"),
        ["Notify.OverheatMessage"] = (
            "{0} temperature reached {1:F0}°C (threshold {2}°C).",
            "Температура {0} достигла {1:F0}°C (порог {2}°C)."),
        ["Tooltip.NoPerCoreData"] = ("Per-core data unavailable", "Данные по ядрам недоступны"),
        ["CoreType.Performance"] = ("Performance (P)", "Производительные (P)"),
        ["CoreType.Efficiency"] = ("Efficiency (E)", "Эффективные (E)"),
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
            "Installation failed. Try running as Administrator, or install PawnIO manually from pawnio.eu.",
            "Не удалось установить. Попробуйте запустить от имени администратора или установите PawnIO вручную с pawnio.eu."),
        ["Update.Title"] = ("Update — HardwareTempWidget", "Обновление — HardwareTempWidget"),
        ["Update.CurrentVersion"] = ("Current version: {0}", "Текущая версия: {0}"),
        ["Update.CheckNow"] = ("Check for updates", "Проверить обновления"),
        ["Update.Checking"] = ("Checking for updates…", "Проверка обновлений…"),
        ["Update.UpToDate"] = ("You're running the latest version.", "Установлена последняя версия."),
        ["Update.Available"] = ("Version {0} is available.", "Доступна версия {0}."),
        ["Update.DownloadInstall"] = ("Download & install", "Скачать и установить"),
        ["Update.Downloading"] = ("Downloading…", "Загрузка…"),
        ["Update.Installed"] = ("Update installed. Restarting…", "Обновление установлено. Перезапуск…"),
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
