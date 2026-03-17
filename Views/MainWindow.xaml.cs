using BookWriter.ViewModels;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

namespace BookWriter.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel Vm => (MainViewModel)DataContext;
        private RichTextBox? _editor;
        private DispatcherTimer? _clock;
        private bool _suppressTextChanged = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();

            Vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.CurrentDocument))
                    UpdateEditorDocument();
            };

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _editor = FindName("Editor") as RichTextBox;
            if (_editor == null) return;

            UpdateEditorDocument();

            Vm.OpenFindDelegate = OpenSearchPanel;

            _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _clock.Tick += (_, _) => UpdateClock();
            _clock.Start();
            UpdateClock();
        }

        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressTextChanged) return;
            try
            {
                // Save RTF directly from the editor control — never from FlowDocument.Blocks
                // (RichTextBox owns the FlowDocument; reading Blocks from outside returns empty)
                if (_editor != null && Vm.SelectedChapter != null)
                {
                    using var ms = new System.IO.MemoryStream();
                    var range = new TextRange(_editor.Document.ContentStart, _editor.Document.ContentEnd);
                    range.Save(ms, DataFormats.Rtf);
                    Vm.SelectedChapter.Model.RtfContent = Convert.ToBase64String(ms.ToArray());
                    Vm.SelectedChapter.Model.UpdatedAt  = DateTime.Now;
                    Vm.SelectedChapter.Model.IsModified = true;
                }
                Vm.NotifyTextChanged();
            }
            catch { }
        }

        private void UpdateEditorDocument()
        {
            if (_editor == null) return;
            try
            {
                _suppressTextChanged = true;

                var chapter = Vm.SelectedChapter?.Model;
                var freshDoc = new FlowDocument();

                if (chapter != null && !string.IsNullOrEmpty(chapter.RtfContent))
                {
                    try
                    {
                        var bytes = Convert.FromBase64String(chapter.RtfContent);
                        using var ms = new System.IO.MemoryStream(bytes);
                        var range = new TextRange(freshDoc.ContentStart, freshDoc.ContentEnd);
                        range.Load(ms, DataFormats.Rtf);
                    }
                    catch { /* leave empty doc */ }
                }

                _editor.Document = freshDoc;
                // Keep chapter.Document in sync so word count etc. work
                if (chapter != null) chapter.Document = freshDoc;
                ApplyTextColor(freshDoc);
            }
            catch { }
            finally { _suppressTextChanged = false; }
        }

        private void ApplyTextColor(FlowDocument doc)
        {
            if (doc == null) return;
            try
            {
                var brush = Application.Current.Resources["PrimaryText"]
                    as System.Windows.Media.SolidColorBrush;
                if (brush == null) return;

                foreach (var block in doc.Blocks)
                {
                    if (block is Paragraph para)
                    {
                        para.Foreground = brush;
                        foreach (var inline in para.Inlines)
                            if (inline is Run run)
                                run.Foreground = brush;
                    }
                }
            }
            catch { }
        }

        private void UpdateClock()
        {
            try
            {
                if (FindName("ClockText") is TextBlock tb)
                    tb.Text = $"// {DateTime.Now:HH:mm}";
            }
            catch { }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (Vm.IsDirty)
            {
                var result = MessageBox.Show(
                    $"В книге «{Vm.BookTitle}» есть несохранённые изменения.\n\nСохранить перед закрытием?",
                    "Несохранённые изменения",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                if (result == MessageBoxResult.Yes)
                    Vm.SaveCommand.Execute(null);
            }

            _clock?.Stop();
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _clock?.Stop();
            base.OnClosed(e);
        }
    }

        // ── Search / Replace ─────────────────────────────────────────

        public void OpenSearchPanel()
        {
            if (FindName("SearchPanel") is System.Windows.Controls.Border panel)
            {
                panel.Visibility = Visibility.Visible;
                if (FindName("SearchBox") is System.Windows.Controls.TextBox box)
                {
                    box.Focus();
                    box.SelectAll();
                }
            }
        }

        private void CloseSearch_Click(object s, RoutedEventArgs e)
        {
            if (FindName("SearchPanel") is System.Windows.Controls.Border panel)
                panel.Visibility = Visibility.Collapsed;
            _editor?.Focus();
        }

        private void SearchBox_KeyDown(object s, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Shift)
                    FindInEditor(forward: false);
                else
                    FindInEditor(forward: true);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                CloseSearch_Click(s, e);
                e.Handled = true;
            }
        }

        private void FindNext_Click(object s, RoutedEventArgs e) => FindInEditor(forward: true);
        private void FindPrev_Click(object s, RoutedEventArgs e) => FindInEditor(forward: false);

        private void FindInEditor(bool forward)
        {
            if (_editor == null) return;
            var searchBox = FindName("SearchBox") as System.Windows.Controls.TextBox;
            var query = searchBox?.Text;
            if (string.IsNullOrEmpty(query)) return;

            var doc = _editor.Document;
            var startPos = forward
                ? (_editor.Selection.IsEmpty ? doc.ContentStart : _editor.Selection.End)
                : (_editor.Selection.IsEmpty ? doc.ContentEnd  : _editor.Selection.Start);

            TextPointer? found = forward
                ? FindText(startPos, doc.ContentEnd, query)
                : FindTextBackward(startPos, doc.ContentStart, query);

            // Wrap around
            if (found == null)
                found = forward
                    ? FindText(doc.ContentStart, doc.ContentEnd, query)
                    : FindTextBackward(doc.ContentEnd, doc.ContentStart, query);

            if (found != null)
            {
                var end = found.GetPositionAtOffset(query.Length, LogicalDirection.Forward);
                if (end != null)
                {
                    _editor.Selection.Select(found, end);
                    found.Paragraph?.BringIntoView();
                    UpdateSearchResult(query);
                }
            }
            else
            {
                UpdateSearchResult(query, notFound: true);
            }
        }

        private void Replace_Click(object s, RoutedEventArgs e)
        {
            if (_editor == null) return;
            var replaceBox = FindName("ReplaceBox") as System.Windows.Controls.TextBox;
            var searchBox  = FindName("SearchBox")  as System.Windows.Controls.TextBox;
            var query   = searchBox?.Text;
            var replace = replaceBox?.Text ?? "";
            if (string.IsNullOrEmpty(query)) return;

            if (!_editor.Selection.IsEmpty &&
                string.Equals(_editor.Selection.Text, query, StringComparison.OrdinalIgnoreCase))
            {
                _editor.Selection.Text = replace;
            }
            FindInEditor(forward: true);
        }

        private void ReplaceAll_Click(object s, RoutedEventArgs e)
        {
            if (_editor == null) return;
            var replaceBox = FindName("ReplaceBox") as System.Windows.Controls.TextBox;
            var searchBox  = FindName("SearchBox")  as System.Windows.Controls.TextBox;
            var query   = searchBox?.Text;
            var replace = replaceBox?.Text ?? "";
            if (string.IsNullOrEmpty(query)) return;

            int count = 0;
            var doc = _editor.Document;
            TextPointer pos = doc.ContentStart;
            while (true)
            {
                var found = FindText(pos, doc.ContentEnd, query);
                if (found == null) break;
                var end = found.GetPositionAtOffset(query.Length, LogicalDirection.Forward);
                if (end == null) break;
                var sel = new TextRange(found, end);
                sel.Text = replace;
                pos = sel.End;
                count++;
            }

            if (FindName("SearchResultText") is System.Windows.Controls.TextBlock tb)
                tb.Text = count > 0 ? $"Заменено: {count}" : "Не найдено";
        }

        private void UpdateSearchResult(string query, bool notFound = false)
        {
            if (FindName("SearchResultText") is System.Windows.Controls.TextBlock tb)
                tb.Text = notFound ? "// NOT FOUND" : "";
        }

        private static TextPointer? FindText(TextPointer start, TextPointer end, string query)
        {
            var pos = start;
            while (pos != null && pos.CompareTo(end) < 0)
            {
                if (pos.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    var run = pos.GetTextInRun(LogicalDirection.Forward);
                    int idx = run.IndexOf(query, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                        return pos.GetPositionAtOffset(idx);
                }
                pos = pos.GetNextContextPosition(LogicalDirection.Forward);
            }
            return null;
        }

        private static TextPointer? FindTextBackward(TextPointer start, TextPointer end, string query)
        {
            var pos = start;
            TextPointer? last = null;
            while (pos != null && pos.CompareTo(end) > 0)
            {
                if (pos.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.Text)
                {
                    var run = pos.GetTextInRun(LogicalDirection.Backward);
                    int idx = run.LastIndexOf(query, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        last = pos.GetPositionAtOffset(-(run.Length - idx));
                        break;
                    }
                }
                pos = pos.GetNextContextPosition(LogicalDirection.Backward);
            }
            return last;
        }

    }
