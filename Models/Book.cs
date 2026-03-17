using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BookWriter.Models
{
    public class Book : INotifyPropertyChanged
    {
        private string _title = "Без названия";
        private string _author = string.Empty;
        private string _description = string.Empty;

        public Guid   Id           { get; set; } = Guid.NewGuid();
        public string Language     { get; set; } = "ru";
        public string Publisher    { get; set; } = string.Empty;
        public string Genre        { get; set; } = string.Empty;
        public string Keywords     { get; set; } = string.Empty;
        public DateTime CreatedDate  { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public List<Chapter>  Chapters { get; set; } = new();
        public Cover          Cover    { get; set; } = new();
        public BookSettings   Settings { get; set; } = new();
        public string? FilePath { get; set; }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string Author
        {
            get => _author;
            set { _author = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        /// <summary>Total word count across all chapters.</summary>
        public int TotalWordCount =>
            Chapters.Sum(c => CountWords(c.GetPlainText()));

        /// <summary>Estimated reading time in minutes (avg 200 wpm).</summary>
        public int EstimatedReadMinutes => Math.Max(1, TotalWordCount / 200);

        public string Summary =>
            $"{Chapters.Count} глав · {TotalWordCount:N0} слов · ~{EstimatedReadMinutes} мин чтения";

        private static int CountWords(string text) =>
            string.IsNullOrWhiteSpace(text) ? 0
            : text.Split(new[] { ' ', '\n', '\r', '\t' },
                         StringSplitOptions.RemoveEmptyEntries).Length;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class BookSettings
    {
        // Typography
        public string FontFamily   { get; set; } = "Consolas";
        public double FontSize     { get; set; } = 13;
        public double LineHeight   { get; set; } = 1.7;
        public string TextColor    { get; set; } = "#C8E6FF";

        // Page layout (mm)
        public double PageWidth    { get; set; } = 148;   // A5
        public double PageHeight   { get; set; } = 210;
        public double MarginTop    { get; set; } = 20;
        public double MarginBottom { get; set; } = 20;
        public double MarginLeft   { get; set; } = 25;
        public double MarginRight  { get; set; } = 15;

        // Header / Footer
        public bool   ShowPageNumbers  { get; set; } = true;
        public bool   ShowChapterTitle { get; set; } = true;
        public bool   ShowBookTitle    { get; set; } = true;

        // Theme
        public bool   IsDarkTheme     { get; set; } = true;
        public string ThemeVariant    { get; set; } = "Cyan"; // Cyan | Pink

        // Auto-save
        public bool   AutoSaveEnabled { get; set; } = true;
        public int    AutoSaveSeconds { get; set; } = 120;

        // AI defaults
        public string AiLanguage   { get; set; } = "ru";
        public string AiModel      { get; set; } = "deepseek-chat";
        public double AiTemperature { get; set; } = 0.8;
        public int    AiMaxTokens  { get; set; } = 2000;
    }
}
