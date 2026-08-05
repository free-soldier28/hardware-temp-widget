# HardwareTempWidget — план реализации

Windows-виджет для отображения температуры CPU и GPU в реальном времени, с архитектурой,
допускающей расширение на другие платформы в будущем.

## Стек

- .NET 8 (LTS)
- Avalonia UI 11.x (Fluent тема, встроенный `TrayIcon`)
- `LibreHardwareMonitorLib` (NuGet) — чтение сенсоров на Windows
- JSON-файл в `%AppData%` для настроек

## Структура решения

```
HardwareTempWidget/
├── HardwareTempWidget.sln
├── src/
│   ├── HardwareTempWidget.Core/            # интерфейсы, модели, поллинг — платформонезависимо
│   │   ├── ISensorProvider.cs
│   │   ├── SensorReading.cs
│   │   ├── SensorPollingService.cs
│   │   └── IAutostartService.cs
│   ├── HardwareTempWidget.Sensors.Windows/ # реализация под Windows
│   │   ├── WindowsSensorProvider.cs        # обёртка над LibreHardwareMonitorLib
│   │   └── WindowsAutostartService.cs      # HKCU Run key
│   └── HardwareTempWidget.App/             # Avalonia UI
│       ├── MainWindow.axaml(.cs)           # borderless, always-on-top виджет
│       ├── SettingsWindow.axaml(.cs)
│       ├── TrayIconSetup.cs
│       └── App.axaml(.cs)
└── tests/
    └── HardwareTempWidget.Core.Tests/      # xUnit, мок ISensorProvider
```

## Архитектурные решения

- `ISensorProvider.GetReadingsAsync()` возвращает `IEnumerable<SensorReading>`
  (Name, Type: CPU/GPU, Value °C). Позволяет позже добавить `LinuxSensorProvider` /
  `MacSensorProvider` без изменения UI.
- Провайдер выбирается фабрикой по `OperatingSystem.IsWindows()`.
- `SensorPollingService` — фоновый таймер (интервал настраивается, по умолчанию 1–2 сек),
  публикует обновления через событие / `ObservableProperty`.
- `IAutostartService` аналогично абстрагирован (Windows: реестр Run; заготовка под
  systemd/launchd на будущее).

## UI/UX

- Главное окно: без рамки, `Topmost`, полупрозрачный фон, компактный размер (~220×100),
  перетаскивание за фон, позиция сохраняется между запусками.
- Крупные цифры CPU/GPU температур с цветовой индикацией порогов (зелёный/жёлтый/красный).
- Правый клик → контекстное меню: Настройки, Автозапуск, Прозрачность, Выход.
- Иконка в трее с тултипом (текущие температуры), клик — показать/скрыть виджет.
- Окно настроек: интервал опроса, прозрачность, автозапуск, тема.

## Особенности/риски

- `LibreHardwareMonitorLib` для полного доступа к сенсорам (особенно GPU и некоторые
  CPU-сенсоры) может требовать **прав администратора** — предусмотреть `app.manifest`
  с `requestedExecutionLevel` или мягкий запрос повышения прав с понятным сообщением
  пользователю.
- Публикация: `dotnet publish -r win-x64 --self-contained` для отдельного exe.

## Порядок реализации (milestones)

1. Скаффолд solution + git-репозиторий
2. `Core`: интерфейсы и модели
3. `Sensors.Windows`: провайдер, проверка через консольный тест на реальных данных
4. Avalonia-приложение: окно с живыми данными (без стилизации)
5. Трей-иконка + контекстное меню
6. Стилизация виджета: borderless, drag-to-move, сохранение позиции
7. Настройки: прозрачность, интервал, автозапуск
8. Упаковка (self-contained publish, манифест для прав)
9. Финальное тестирование на реальном железе
