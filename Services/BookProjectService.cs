using BookWriter.Models;
using Newtonsoft.Json;
using System.IO;

namespace BookWriter.Services
{
    public class BookProjectService
    {
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Formatting        = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatString  = "yyyy-MM-dd HH:mm:ss",
            // Tolerate errors in individual fields
            Error = (sender, args) =>
            {
                args.ErrorContext.Handled = true;
            }
        };

        private static readonly string RecentFilesPath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BookWriter", "recent.json");

        // ── Save ──────────────────────────────────────────────────────

        public void Save(Book book, string filePath)
        {
            foreach (var chapter in book.Chapters)
            {
                try { chapter.SaveDocumentToRtf(); }
                catch { /* skip broken chapter */ }
            }

            book.ModifiedDate = DateTime.Now;
            book.FilePath     = filePath;

            // Sanitize cover base64 before saving
            SanitizeCover(book.Cover);

            var json = JsonConvert.SerializeObject(book, JsonSettings);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

            AddRecentFile(filePath, book.Title);
        }

        // ── Load ──────────────────────────────────────────────────────

        public Book Load(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл не найден: {filePath}");

            var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);

            Book? book = null;
            try
            {
                book = JsonConvert.DeserializeObject<Book>(json, JsonSettings);
            }
            catch (Exception ex)
            {
                // Try to recover by stripping cover data
                throw new InvalidDataException(
                    $"Файл повреждён. Попробуйте открыть его в текстовом редакторе и удалить поля FrontCoverBase64/BackCoverBase64.\n\nДетали: {ex.Message}");
            }

            if (book == null)
                throw new InvalidDataException("Не удалось прочитать файл книги.");

            book.FilePath = filePath;

            // Sanitize cover (remove invalid base64)
            SanitizeCover(book.Cover);

            var loadErrors = new List<string>();
            foreach (var chapter in book.Chapters.OrderBy(c => c.Order))
            {
                try
                {
                    chapter.LoadDocumentFromRtf();
                }
                catch (Exception chEx)
                {
                    // Chapter RTF is corrupt — create empty document so the chapter is at least visible
                    loadErrors.Add($"Глава '{chapter.Title}': {chEx.Message}");
                    chapter.Document = new System.Windows.Documents.FlowDocument();
                    chapter.Document.Blocks.Add(new System.Windows.Documents.Paragraph(
                        new System.Windows.Documents.Run($"[Не удалось загрузить содержимое: {chEx.Message}]")));
                }
            }
            if (loadErrors.Count > 0)
                System.Windows.MessageBox.Show(
                    $"Некоторые главы не удалось загрузить корректно:\n\n{string.Join("\n", loadErrors)}",
                    "Предупреждение", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);

            AddRecentFile(filePath, book.Title);
            return book;
        }

        // ── Load with recovery ────────────────────────────────────────

        /// <summary>
        /// Tries to load, and if cover base64 is corrupt — strips it and retries.
        /// </summary>
        public Book LoadWithRecovery(string filePath)
        {
            try
            {
                return Load(filePath);
            }
            catch
            {
                // Strip cover fields and retry
                var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                var cleaned = StripCoverBase64(json);
                var book = JsonConvert.DeserializeObject<Book>(cleaned, JsonSettings)
                    ?? throw new InvalidDataException("Файл не восстановим.");
                book.FilePath = filePath;
                foreach (var ch in book.Chapters.OrderBy(c => c.Order))
                {
                    try { ch.LoadDocumentFromRtf(); }
                    catch
                    {
                        ch.Document = new System.Windows.Documents.FlowDocument();
                    }
                }
                return book;
            }
        }

        // ── Create new ────────────────────────────────────────────────

        public Book CreateNew(string title = "Новая книга", string author = "")
        {
            var book = new Book { Title = title, Author = author };
            var chapter = new Chapter { Title = "Глава 1", Order = 0 };
            chapter.LoadDocumentFromRtf();
            book.Chapters.Add(chapter);
            return book;
        }

        // ── Recent files ─────────────────────────────────────────────

        public List<RecentFile> GetRecentFiles()
        {
            try
            {
                if (!File.Exists(RecentFilesPath)) return new();
                var json = File.ReadAllText(RecentFilesPath);
                return JsonConvert.DeserializeObject<List<RecentFile>>(json) ?? new();
            }
            catch { return new(); }
        }

        private void AddRecentFile(string path, string title)
        {
            try
            {
                var list = GetRecentFiles();
                list.RemoveAll(r => r.Path == path);
                list.Insert(0, new RecentFile { Path = path, Title = title, OpenedAt = DateTime.Now });
                if (list.Count > 20) list = list.Take(20).ToList();
                Directory.CreateDirectory(Path.GetDirectoryName(RecentFilesPath)!);
                File.WriteAllText(RecentFilesPath,
                    JsonConvert.SerializeObject(list, Formatting.Indented));
            }
            catch { }
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static void SanitizeCover(Cover cover)
        {
            if (cover == null) return;
            if (!IsValidBase64(cover.FrontCoverBase64))
                cover.FrontCoverBase64 = null;
            if (!IsValidBase64(cover.BackCoverBase64))
                cover.BackCoverBase64 = null;
        }

        private static bool IsValidBase64(string? s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            try { Convert.FromBase64String(s); return true; }
            catch { return false; }
        }

        private static string StripCoverBase64(string json)
        {
            // Remove FrontCoverBase64 and BackCoverBase64 values using regex
            var result = System.Text.RegularExpressions.Regex.Replace(
                json,
                @"""(FrontCoverBase64|BackCoverBase64)""\s*:\s*""[^""]*""",
                m => $"\"{m.Groups[1].Value}\": null");
            return result;
        }
    }

    public class RecentFile
    {
        public string   Path     { get; set; } = string.Empty;
        public string   Title    { get; set; } = string.Empty;
        public DateTime OpenedAt { get; set; }
    }
}
