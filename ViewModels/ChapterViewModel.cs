using BookWriter.Models;

namespace BookWriter.ViewModels
{
    /// <summary>
    /// ViewModel wrapper for a Chapter model.
    /// Exposes bindable properties for the chapter list in the sidebar.
    /// </summary>
    public class ChapterViewModel : BaseViewModel
    {
        private readonly Chapter _model;

        public Chapter Model => _model;

        public Guid Id => _model.Id;

        public string Title
        {
            get => _model.Title;
            set
            {
                if (_model.Title == value) return;
                _model.Title = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }

        public int Order
        {
            get => _model.Order;
            set
            {
                if (_model.Order == value) return;
                _model.Order = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }

        public string Status
        {
            get => _model.Status;
            set { _model.Status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); }
        }

        public string AuthorNotes
        {
            get => _model.AuthorNotes;
            set { _model.AuthorNotes = value; OnPropertyChanged(); }
        }

        /// <summary>Formatted display: "#1 — Chapter Title"</summary>
        public string DisplayTitle => $"#{Order + 1} — {Title}";

        public int WordCount => _model.WordCount;

        public string WordCountLabel => WordCount > 0 ? $"{WordCount:N0}w" : string.Empty;

        public string Preview => _model.GetPreview(100);

        /// <summary>Color indicator for chapter status.</summary>
        public string StatusColor => Status switch
        {
            "Final"  => "#00FFCC",
            "Review" => "#FFE600",
            _        => "#3A6080"   // Draft
        };

        public bool IsModified => _model.IsModified;

        public string UpdatedLabel =>
            _model.UpdatedAt > DateTime.MinValue
                ? _model.UpdatedAt.ToString("dd.MM HH:mm")
                : string.Empty;

        public ChapterViewModel(Chapter model)
        {
            _model = model;
        }

        public void RefreshStats()
        {
            OnPropertyChanged(nameof(WordCount));
            OnPropertyChanged(nameof(WordCountLabel));
            OnPropertyChanged(nameof(Preview));
            OnPropertyChanged(nameof(IsModified));
            OnPropertyChanged(nameof(UpdatedLabel));
        }

        public override string ToString() => DisplayTitle;
    }
}
