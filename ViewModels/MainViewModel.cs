using BookWriter.Commands;
using BookWriter.Data;
using BookWriter.Models;
using BookWriter.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace BookWriter.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // ── Services ─────────────────────────────────────────────────────
        private readonly BookProjectService _projectService = new();
        private readonly PdfExportService   _pdfService     = new();
        private readonly EpubExportService  _epubService    = new();
        private readonly AutoSaveService    _autoSave;
        private readonly ThemeService       _themeService   = new();
        private readonly BookRepository     _repo           = new();

        // ── State ────────────────────────────────────────────────────────
        private Book              _book;
        private ChapterViewModel? _selectedChapter;
        private string            _statusText    = "// READY";
        private string            _dbStatsText   = string.Empty;
        private bool              _isDirty;
        private bool              _isAiPanelOpen = false;
        private int               _wordCount;

        // ── Properties ───────────────────────────────────────────────────
        public Book Book { get => _book; private set => SetProperty(ref _book, value); }

        public string BookTitle
        {
            get => _book.Title;
            set { _book.Title = value; OnPropertyChanged(); SetDirty(); }
        }

        public string BookAuthor
        {
            get => _book.Author;
            set { _book.Author = value; OnPropertyChanged(); SetDirty(); }
        }

        public string StatusText  { get => _statusText;  set => SetProperty(ref _statusText,  value); }
        public string DbStatsText { get => _dbStatsText; set => SetProperty(ref _dbStatsText, value); }
        public bool   IsDirty     { get => _isDirty;     set => SetProperty(ref _isDirty,     value); }

        public bool IsAiPanelOpen
        {
            get => _isAiPanelOpen;
            set { SetProperty(ref _isAiPanelOpen, value); OnPropertyChanged(nameof(AiPanelWidth)); }
        }
        public GridLength AiPanelWidth => new GridLength(0); // AI removed

        public string WordCountText => $"слов: {_wordCount:N0}";

        public ObservableCollection<ChapterViewModel> Chapters { get; } = new();

        public ChapterViewModel? SelectedChapter
        {
            get => _selectedChapter;
            set
            {
                if (_selectedChapter != null)
                    _selectedChapter.Model.SaveDocumentToRtf();
                SetProperty(ref _selectedChapter, value);
                OnPropertyChanged(nameof(CurrentDocument));
                UpdateWordCount();
            }
        }

        public FlowDocument? CurrentDocument => _selectedChapter?.Model.Document;

        // Delegates set by code-behind
        public Func<string>?         GetSelectedTextDelegate { get; set; }
        public Action<string, bool>? InsertTextDelegate      { get; set; }
        public Action?               OpenFindDelegate        { get; set; }

        // ── Commands ─────────────────────────────────────────────────────
        public ICommand NewCommand             { get; }
        public ICommand OpenCommand            { get; }
        public ICommand SaveCommand            { get; }
        public ICommand SaveAsCommand          { get; }
        public ICommand ExportPdfCommand       { get; }
        public ICommand ExportEpubCommand      { get; }
        public ICommand PrintCommand           { get; }
        public ICommand ToggleThemeCommand     { get; }
        public ICommand AddChapterCommand      { get; }
        public ICommand DeleteChapterCommand   { get; }
        public ICommand MoveChapterUpCommand   { get; }
        public ICommand MoveChapterDownCommand { get; }
        public ICommand RenameChapterCommand   { get; }
        public ICommand OpenSettingsCommand    { get; }
        public ICommand AboutCommand           { get; }
        public ICommand OpenCoverEditorCommand { get; }
        public ICommand OpenLibraryCommand     { get; }
        public ICommand OpenRevisionsCommand   { get; }
        public ICommand SaveRevisionCommand    { get; }
        public ICommand InsertHeadingCommand   { get; }
        public ICommand OpenPreviewCommand     { get; }
        public ICommand OpenFindCommand        { get; }

        // ── Constructor ──────────────────────────────────────────────────
        public MainViewModel()
        {
            _autoSave = new AutoSaveService(_projectService);
            _autoSave.AutoSaved += (_, _) =>
                StatusText = $"// AUTOSAVE {DateTime.Now:HH:mm}";

            _book = _projectService.CreateNew();
            LoadChapters();
            RefreshDbStats();

            NewCommand             = new RelayCommand(ExecuteNew);
            OpenCommand            = new RelayCommand(ExecuteOpen);
            SaveCommand            = new RelayCommand(ExecuteSave);
            SaveAsCommand          = new RelayCommand(ExecuteSaveAs);
            ExportPdfCommand       = new RelayCommand(ExecuteExportPdf);
            ExportEpubCommand      = new RelayCommand(ExecuteExportEpub);
            PrintCommand           = new RelayCommand(ExecutePrint);
            ToggleThemeCommand     = new RelayCommand(_ => { _themeService.Toggle(); _book.Settings.IsDarkTheme = _themeService.IsDarkTheme; });
            AddChapterCommand      = new RelayCommand(ExecuteAddChapter);
            DeleteChapterCommand   = new RelayCommand(ExecuteDeleteChapter,  _ => SelectedChapter != null && Chapters.Count > 1);
            MoveChapterUpCommand   = new RelayCommand(ExecuteMoveUp,         _ => SelectedChapter != null && Chapters.IndexOf(SelectedChapter) > 0);
            MoveChapterDownCommand = new RelayCommand(ExecuteMoveDown,       _ => SelectedChapter != null && Chapters.IndexOf(SelectedChapter) < Chapters.Count - 1);
            RenameChapterCommand   = new RelayCommand(ExecuteRenameChapter,  _ => SelectedChapter != null);
            OpenSettingsCommand    = new RelayCommand(ExecuteOpenSettings);
            AboutCommand           = new RelayCommand(ExecuteAbout);
            OpenCoverEditorCommand = new RelayCommand(ExecuteOpenCoverEditor);
            OpenLibraryCommand     = new RelayCommand(_ => StatusText = "// LIBRARY — coming soon");
            OpenRevisionsCommand   = new RelayCommand(_ => StatusText = "// REVISIONS — coming soon");
            SaveRevisionCommand    = new RelayCommand(ExecuteSaveRevision, _ => SelectedChapter != null);
            InsertHeadingCommand   = new RelayCommand(ExecuteInsertHeading);
            OpenPreviewCommand     = new RelayCommand(ExecuteOpenPreview);
            OpenFindCommand        = new RelayCommand(_ => OpenFindDelegate?.Invoke());
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private void LoadChapters()
        {
            Chapters.Clear();
            foreach (var ch in _book.Chapters.OrderBy(c => c.Order))
                Chapters.Add(new ChapterViewModel(ch));
            SelectedChapter = Chapters.FirstOrDefault();
        }

        private void SetDirty() => IsDirty = true;

        private void SyncChapterOrder()
        {
            for (int i = 0; i < Chapters.Count; i++) Chapters[i].Order = i;
        }

        private void UpdateWordCount()
        {
            try
            {
                var text = SelectedChapter?.Model.GetPlainText() ?? string.Empty;
                _wordCount = string.IsNullOrWhiteSpace(text) ? 0
                    : text.Split(new[] { ' ', '\n', '\r', '\t' },
                                  StringSplitOptions.RemoveEmptyEntries).Length;
            }
            catch { _wordCount = 0; }
            OnPropertyChanged(nameof(WordCountText));
        }

        public void NotifyTextChanged()
        {
            UpdateWordCount();
            _selectedChapter?.RefreshStats();
        }

        private void RefreshDbStats()
        {
            try
            {
                var (books, chapters, words) = _repo.GetLibraryStats();
                DbStatsText = $"db: {books}кн / {chapters}гл / {words:N0}сл";
            }
            catch { DbStatsText = "db: offline"; }
        }

        // ── Commands ─────────────────────────────────────────────────────
        private void ExecuteNew(object? _)
        {
            if (IsDirty && !ConfirmDiscard()) return;
            _autoSave.Detach();
            _book = _projectService.CreateNew();
            IsDirty = false;
            LoadChapters();
            OnPropertyChanged(nameof(BookTitle));
            StatusText = "// NEW BOOK";
        }

        private void ExecuteOpen(object? _)
        {
            if (IsDirty && !ConfirmDiscard()) return;
            var dlg = new OpenFileDialog
            {
                Filter = "Book Project (*.bookproject)|*.bookproject",
                Title  = "Открыть книгу"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                _autoSave.Detach();
                _book = _projectService.LoadWithRecovery(dlg.FileName);
                IsDirty = false;
                LoadChapters();
                _autoSave.Attach(_book, dlg.FileName);
                OnPropertyChanged(nameof(BookTitle));
                StatusText = $"// OPENED: {System.IO.Path.GetFileName(dlg.FileName)}";
                RefreshDbStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteSave(object? _)
        {
            if (string.IsNullOrEmpty(_book.FilePath)) { ExecuteSaveAs(null); return; }
            SaveToFile(_book.FilePath);
        }

        private void ExecuteSaveAs(object? _)
        {
            var dlg = new SaveFileDialog
            {
                Filter   = "Book Project (*.bookproject)|*.bookproject",
                FileName = _book.Title
            };
            if (dlg.ShowDialog() != true) return;
            SaveToFile(dlg.FileName);
            _autoSave.Attach(_book, dlg.FileName);
        }

        private void SaveToFile(string path)
        {
            try
            {
                foreach (var ch in Chapters) ch.Model.SaveDocumentToRtf();
                _projectService.Save(_book, path);
                try { _repo.UpsertBook(_book); RefreshDbStats(); } catch { }
                IsDirty = false;
                StatusText = $"// SAVED: {System.IO.Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExportPdf(object? _)
        {
            foreach (var ch in Chapters) ch.Model.SaveDocumentToRtf();
            var dlg = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = _book.Title };
            if (dlg.ShowDialog() != true) return;
            try
            {
                StatusText = "// EXPORTING PDF…";
                _pdfService.Export(_book, dlg.FileName);
                StatusText = "// PDF DONE";
                if (MessageBox.Show("PDF создан. Открыть?", "OK",
                    MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                StatusText = "// PDF ERROR";
                MessageBox.Show($"Ошибка PDF:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExportEpub(object? _)
        {
            foreach (var ch in Chapters) ch.Model.SaveDocumentToRtf();
            var dlg = new SaveFileDialog { Filter = "EPUB (*.epub)|*.epub", FileName = _book.Title };
            if (dlg.ShowDialog() != true) return;
            try
            {
                StatusText = "// EXPORTING EPUB…";
                _epubService.Export(_book, dlg.FileName);
                StatusText = "// EPUB DONE";
                MessageBox.Show("EPUB создан!", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText = "// EPUB ERROR";
                MessageBox.Show($"Ошибка EPUB:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecutePrint(object? _)
        {
            var dlg = new PrintDialog();
            if (dlg.ShowDialog() != true) return;
            var doc = CurrentDocument ?? new FlowDocument(new Paragraph(new Run("Пусто")));
            dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, _book.Title);
        }

        private void ExecuteAddChapter(object? _)
        {
            var ch = new Chapter { Title = $"Глава {Chapters.Count + 1}", Order = Chapters.Count };
            ch.LoadDocumentFromRtf();
            _book.Chapters.Add(ch);
            var vm = new ChapterViewModel(ch);
            Chapters.Add(vm);
            SelectedChapter = vm;
            SetDirty();
        }

        private void ExecuteDeleteChapter(object? _)
        {
            if (SelectedChapter == null) return;
            if (MessageBox.Show($"Удалить «{SelectedChapter.Title}»?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            _book.Chapters.Remove(SelectedChapter.Model);
            Chapters.Remove(SelectedChapter);
            SyncChapterOrder();
            SelectedChapter = Chapters.FirstOrDefault();
            SetDirty();
        }

        private void ExecuteMoveUp(object? _)
        {
            if (SelectedChapter == null) return;
            int i = Chapters.IndexOf(SelectedChapter);
            if (i > 0) { Chapters.Move(i, i - 1); SyncChapterOrder(); SetDirty(); }
        }

        private void ExecuteMoveDown(object? _)
        {
            if (SelectedChapter == null) return;
            int i = Chapters.IndexOf(SelectedChapter);
            if (i < Chapters.Count - 1) { Chapters.Move(i, i + 1); SyncChapterOrder(); SetDirty(); }
        }

        private void ExecuteRenameChapter(object? _)
        {
            if (SelectedChapter == null) return;
            var dlg = new Views.InputDialog("Переименовать главу", "Название:", SelectedChapter.Title);
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.Result))
            { SelectedChapter.Title = dlg.Result; SetDirty(); }
        }

        private void ExecuteOpenSettings(object? _)
        {
            var dlg = new Views.SettingsWindow(_book.Settings, _themeService);
            dlg.Owner = Application.Current.MainWindow;
            if (dlg.ShowDialog() == true) SetDirty();
        }

        private void ExecuteOpenCoverEditor(object? _)
        {
            var dlg = new Views.CoverEditorWindow(_book);
            dlg.Owner = Application.Current.MainWindow;
            if (dlg.ShowDialog() == true) SetDirty();
        }

        private void ExecuteSaveRevision(object? _)
        {
            if (SelectedChapter == null) return;
            SelectedChapter.Model.SaveDocumentToRtf();
            try
            {
                _repo.SaveRevision(SelectedChapter.Model.Id,
                    SelectedChapter.Model.RtfContent,
                    $"manual {DateTime.Now:dd.MM HH:mm}");
                StatusText = "// REVISION SAVED";
            }
            catch { StatusText = "// REVISION FAILED"; }
        }

        private void ExecuteInsertHeading(object? param)
        {
            StatusText = $"// H{param}";
        }

        private void ExecuteOpenPreview(object? _)
        {
            _selectedChapter?.Model.SaveDocumentToRtf();
            try
            {
                var win = new Views.PreviewWindow(_book);
                win.Owner = Application.Current.MainWindow;
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка предпросмотра:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteAbout(object? _)
        {
            MessageBox.Show(
                "BOOKWRITER v2.0\n\n" +
                "► Редактор книг с cyberpunk-интерфейсом\n" +
                "► SQLite библиотека\n" +
                "► Экспорт PDF (A4/A5) и EPUB\n" +
                "► Предпросмотр книги\n" +
                "► Автосохранение + краш-репорты\n\n" +
                "© 2024",
                "// ABOUT", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool ConfirmDiscard()
            => MessageBox.Show("Несохранённые изменения. Продолжить?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning)
               == MessageBoxResult.Yes;
    }
}
