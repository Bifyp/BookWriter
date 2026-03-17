using BookWriter.Models;
using System.IO;

namespace BookWriter.Services
{
    /// <summary>
    /// Periodically saves the current book to its .bookproject file.
    /// Also writes a crash-recovery temp file to AppData every 60 seconds.
    /// </summary>
    public class AutoSaveService : IDisposable
    {
        private readonly BookProjectService _projectService;
        private System.Timers.Timer?        _mainTimer;
        private System.Timers.Timer?        _recoveryTimer;
        private Book?   _book;
        private string? _filePath;
        private bool    _enabled;
        private bool    _disposed;

        private static readonly string RecoveryDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "BookWriter", "recovery");

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                UpdateTimers();
            }
        }

        public int IntervalSeconds { get; set; } = 120;
        public DateTime? LastSavedAt { get; private set; }
        public int SaveCount { get; private set; }

        public event EventHandler<AutoSaveEventArgs>? AutoSaved;
        public event EventHandler<string>?            AutoSaveFailed;

        public AutoSaveService(BookProjectService projectService)
        {
            _projectService = projectService;
            _enabled        = true;

            _mainTimer = new System.Timers.Timer(IntervalSeconds * 1000);
            _mainTimer.Elapsed += (_, _) => TrySave();

            _recoveryTimer = new System.Timers.Timer(60_000); // every 60s
            _recoveryTimer.Elapsed += (_, _) => TryWriteRecovery();

            Directory.CreateDirectory(RecoveryDir);
        }

        // ── Attach / Detach ───────────────────────────────────────────

        public void Attach(Book book, string filePath)
        {
            _book     = book;
            _filePath = filePath;
            UpdateTimers();
        }

        public void Detach()
        {
            _book     = null;
            _filePath = null;
            _mainTimer!.Enabled     = false;
            _recoveryTimer!.Enabled = false;
            DeleteRecoveryFile();
        }

        // ── Force save ────────────────────────────────────────────────

        public bool ForceSave()
        {
            if (_book == null || string.IsNullOrEmpty(_filePath)) return false;
            return TrySave();
        }

        // ── Recovery ─────────────────────────────────────────────────

        public string? FindRecoveryFile(Guid bookId)
        {
            var path = RecoveryFilePath(bookId);
            return File.Exists(path) ? path : null;
        }

        public void DeleteRecoveryFile()
        {
            if (_book == null) return;
            var path = RecoveryFilePath(_book.Id);
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        // ── Private ───────────────────────────────────────────────────

        private bool TrySave()
        {
            if (_book == null || string.IsNullOrEmpty(_filePath)) return false;
            try
            {
                // System.Timers.Timer fires on a thread-pool thread.
                // FlowDocument / RichTextBox belong to the UI thread — marshal everything there.
                var app = System.Windows.Application.Current;
                if (app != null)
                {
                    app.Dispatcher.Invoke(() =>
                    {
                        _projectService.Save(_book, _filePath!);
                        LastSavedAt = DateTime.Now;
                        SaveCount++;
                        AutoSaved?.Invoke(this, new AutoSaveEventArgs(_filePath!, SaveCount));
                    });
                }
                else
                {
                    _projectService.Save(_book, _filePath);
                    LastSavedAt = DateTime.Now;
                    SaveCount++;
                    AutoSaved?.Invoke(this, new AutoSaveEventArgs(_filePath, SaveCount));
                }
                return true;
            }
            catch (Exception ex)
            {
                AutoSaveFailed?.Invoke(this, ex.Message);
                return false;
            }
        }

        private void TryWriteRecovery()
        {
            if (_book == null) return;
            try
            {
                var path = RecoveryFilePath(_book.Id);
                var app  = System.Windows.Application.Current;
                if (app != null)
                    app.Dispatcher.Invoke(() => _projectService.Save(_book, path));
                else
                    _projectService.Save(_book, path);
            }
            catch { /* silent */ }
        }

        private static string RecoveryFilePath(Guid bookId) =>
            Path.Combine(RecoveryDir, $"{bookId}.recovery.bookproject");

        private void UpdateTimers()
        {
            if (_mainTimer == null || _recoveryTimer == null) return;
            bool active = _enabled && !string.IsNullOrEmpty(_filePath);
            _mainTimer.Interval     = IntervalSeconds * 1000;
            _mainTimer.Enabled      = active;
            _recoveryTimer.Enabled  = _book != null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _mainTimer?.Dispose();
            _mainTimer = null;
            _recoveryTimer?.Dispose();
            _recoveryTimer = null;
        }
    }

    public class AutoSaveEventArgs : EventArgs
    {
        public string FilePath  { get; }
        public int    SaveCount { get; }
        public AutoSaveEventArgs(string path, int count) { FilePath = path; SaveCount = count; }
    }
}
