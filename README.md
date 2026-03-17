<div align="center">

```
██████╗  ██████╗  ██████╗ ██╗  ██╗    ██╗    ██╗██████╗ ██╗████████╗███████╗██████╗
██╔══██╗██╔═══██╗██╔═══██╗██║ ██╔╝    ██║    ██║██╔══██╗██║╚══██╔══╝██╔════╝██╔══██╗
██████╔╝██║   ██║██║   ██║█████╔╝     ██║ █╗ ██║██████╔╝██║   ██║   █████╗  ██████╔╝
██╔══██╗██║   ██║██║   ██║██╔═██╗     ██║███╗██║██╔══██╗██║   ██║   ██╔══╝  ██╔══██╗
██████╔╝╚██████╔╝╚██████╔╝██║  ██╗    ╚███╔███╔╝██║  ██║██║   ██║   ███████╗██║  ██║
╚═════╝  ╚═════╝  ╚═════╝ ╚═╝  ╚═╝     ╚══╝╚══╝ ╚═╝  ╚═╝╚═╝   ╚═╝   ╚══════╝╚═╝  ╚═╝
```

**`// CYBER-EDITION v1.1`**

*десктопное приложение для писателей · Windows · .NET 8 · cyberpunk-интерфейс*

---

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blueviolet?style=for-the-badge&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Windows](https://img.shields.io/badge/Windows-10%2F11-00FFCC?style=for-the-badge&logo=windows&logoColor=black)](https://github.com)
[![License MIT](https://img.shields.io/badge/License-MIT-FF007A?style=for-the-badge)](LICENSE)
[![Export](https://img.shields.io/badge/Export-PDF%20%7C%20EPUB-00AAFF?style=for-the-badge)](https://www.questpdf.com)
[![SQLite](https://img.shields.io/badge/DB-SQLite-F58233?style=for-the-badge&logo=sqlite&logoColor=white)](https://sqlite.org)

</div>

---

```
  ╔══════════════════════════════════════════════════════════════════════╗
  ║                                                                      ║
  ║   ┌─────────────────┐  ┌────────────────────────────────────────┐    ║
  ║   │  [ CHAPTERS ]   │  │          [ RTF EDITOR ]                │    ║
  ║   │─────────────────│  │────────────────────────────────────────│    ║
  ║   │  Глава 01  ●    │  │  Lorem ipsum dolor sit amet,           │    ║
  ║   │   Глава 02      │  │  consectetur adipiscing elit. Sed do   │    ║
  ║   │   Глава 03      │  │  eiusmod tempor incididunt ut labore   │    ║
  ║   │   Глава 04      │  │  et dolore magna aliqua...             │    ║
  ║   │   + NEW CHAPTER │  │                                        │    ║
  ║   └─────────────────┘  │  _                                     │    ║
  ║                         └────────────────────────────────────────┘   ║
  ║   ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░     ║
  ║   STATUS: // READY   ▌  АВТОСОХРАНЕНИЕ: OK   ▌  СЛОВ: 12,340         ║
  ╚══════════════════════════════════════════════════════════════════════╝
```

---

## `> SYSTEM.FEATURES`

| МОДУЛЬ | ОПИСАНИЕ | СТАТУС |
|:-------|:---------|:------:|
| `RTF_ENGINE` | Жирный · курсив · подчёркивание · заголовки · списки | `[ACTIVE]` |
| `PDF_EXPORT` | Обложка · титульная · колонтитулы · нумерация · A5 | `[ACTIVE]` |
| `EPUB3_EXPORT` | NCX + nav · cover.jpg · xhtml на главу · EPUB2/3 | `[ACTIVE]` |
| `SQLITE_VAULT` | Все книги · история версий · теги · мягкое удаление | `[ACTIVE]` |
| `AUTOSAVE` | Тихое сохранение каждые 2 минуты · без прерываний | `[ACTIVE]` |
| `COVER_EDITOR` | Front + back cover · PNG / JPEG | `[ACTIVE]` |
| `VERSION_CTRL` | До 50 ревизий на главу · откат в один клик | `[ACTIVE]` |
| `DUAL_THEME` | Cyberpunk Cyan `◈` Cyberpunk Pink | `[ACTIVE]` |
| `DRAG_ORDER` | Перестановка глав через ↑ ↓ | `[ACTIVE]` |

---

## `> QUICK.BOOT`

### Требования

```
OS   ─── Windows 10 / 11  (x64)
SDK  ─── .NET 8           https://dotnet.microsoft.com/download/dotnet/8.0
```

### Запуск из исходников

```bash
git clone https://github.com/YOUR_USERNAME/BookWriter.git
cd BookWriter/BookWriter
dotnet run
```

### Сборка в автономный `.exe`

```bash
dotnet publish -c Release -r win-x64   \
  --self-contained true                \
  -p:PublishSingleFile=true            \
  -o ./publish
```

> **Результат:** `publish/BookWriter.exe` — ~80–120 MB, установка .NET не нужна

---

## `> ARCHITECTURE.MAP`

```
BookWriter/
│
├── App.xaml.cs                  QuestPDF license init · SQLite bootstrap
│
├── Models/
│   ├── Book.cs                  настройки: шрифт · размер · язык
│   ├── Chapter.cs               RTF  FlowDocument · wordcount
│   └── Cover.cs                 обложка в байтах
│
├── Data/
│   ├── BookDbContext.cs         EF Core + SQLite
│   ├── BookRepository.cs        CRUD-операции
│   └── DbMigrator.cs            EnsureCreated при старте
│
├── Services/
│   ├── PdfExportService.cs      QuestPDF (Community)
│   ├── EpubExportService.cs   ◀  ZIP-based EPUB 3
│   ├── AutoSaveService.cs     ◀  таймер 2 мин
│   └── BookProjectService.cs  ◀  *.bookproject (JSON + Base64 RTF)
│
├── ViewModels/
│   ├── MainViewModel.cs       ◀  команды · экспорт · навигация
│   └── ChapterViewModel.cs    ◀  VM одной главы
│
├── Views/
│   ├── MainWindow.xaml        ◀  главное окно
│   ├── CoverEditorWindow.xaml ◀  редактор обложки
│   └── SettingsWindow.xaml    ◀  параметры книги
│
└── Themes/
    ├── LightTheme.xaml        ◀  Cyberpunk Cyan
    ├── DarkTheme.xaml         ◀  Cyberpunk Pink
    └── CommonStyles.xaml      ◀  кнопки · скроллбары · списки
```

---

## `> DATABASE.SCHEMA`

**Файл:** `%APPDATA%\BookWriter\library.db`

```
┌──────────────┬────────────────────────────────────────────┐
│   ТАБЛИЦА    │   НАЗНАЧЕНИЕ                               │
├──────────────┼────────────────────────────────────────────┤
│  Books       │  Все книги (soft-delete)                   │
│  Chapters    │  Главы: RTF-контент + wordcount            │
│  Covers      │  JPEG/PNG обложки в BLOB                   │
│  Tags        │  Теги с цветом                             │
│  BookTags    │  Связь книга ↔ тег (M2M)                   │
│  Revisions   │  История версий (≤ 50 на главу)            │
│  AppSettings │  Настройки приложения                      │
└──────────────┴────────────────────────────────────────────┘
```

---

## `> EXPORT.PROTOCOLS`

### `// PDF  ──  via QuestPDF (Community, free)`

```
Pipeline: Обложка → Титульная страница → Главы → Задняя обложка
Format:   A5  ·  нумерация страниц  ·  верхние и нижние колонтитулы
```

### `// EPUB 3  ──  ZIP-based, EPUB 2 compatible`

```
Structure:
  mimetype
  META-INF/container.xml
  OEBPS/
    content.opf         ← манифест
    toc.ncx             ← EPUB 2 навигация
    nav.xhtml           ← EPUB 3 навигация
    chapter_001.xhtml
    chapter_002.xhtml
    ...
    cover.jpg
```

### `// .bookproject  ──  портабельный формат`

```
JSON-файл с RTF-контентом в Base64
Передаётся между машинами без потери форматирования
```

---

## `> HOTKEYS.TABLE`

```
╔══════════════════╦══════════════════════════════╗
║   КОМАНДА        ║   ДЕЙСТВИЕ                   ║
╠══════════════════╬══════════════════════════════╣
║  Ctrl + S        ║  Сохранить                   ║
║  Ctrl + Shift+S  ║  Сохранить как...            ║
║  Ctrl + O        ║  Открыть проект              ║
║  Ctrl + N        ║  Новая книга                 ║
║  Ctrl + P        ║  Печать                      ║
╠══════════════════╬══════════════════════════════╣
║  Ctrl + B        ║  Жирный                      ║
║  Ctrl + I        ║  Курсив                      ║
║  Ctrl + U        ║  Подчёркивание               ║
╚══════════════════╩══════════════════════════════╝
```

---

## `> DEPENDENCIES`

| ПАКЕТ | ВЕРСИЯ | НАЗНАЧЕНИЕ |
|:------|:------:|:-----------|
| `QuestPDF` | 2024.3.4 | PDF-экспорт |
| `Microsoft.EntityFrameworkCore.Sqlite` | 8.0.4 | SQLite ORM |
| `Newtonsoft.Json` | 13.0.3 | JSON-сериализация |
| `Microsoft.Xaml.Behaviors.Wpf` | 1.1.77 | WPF-поведения |

---

## `> CHANGELOG`

### `v1.1  //  PATCH`

```diff
+ EPUB/PDF EXPORT  — все главы экспортируются корректно (не только последняя)
+ PROJECT SAVE     — несохранённые главы флашатся перед записью на диск
+ QUESTPDF LICENSE — LicenseType.Community в App.xaml.cs, popup устранён
```

---

## `> LICENSE`

```
MIT License — делай что хочешь, упомяни автора.
```

---

<div align="center">

```
 ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
 ░                                                    ░
 ░          // JACK IN.  START WRITING.               ░
 ░                                                    ░
 ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
```

*made with `</love>` and too much coffee · MIT · 2024*

</div>
