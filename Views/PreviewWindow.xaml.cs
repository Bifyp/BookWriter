using BookWriter.Models;
using System.Windows;
using System.Windows.Documents;
using System.IO;
using System.Windows.Media;

namespace BookWriter.Views
{
    public partial class PreviewWindow : Window
    {
        private readonly Book _book;
        private readonly List<FlowDocument> _pages = new();
        private int _currentPage = 0;

        public PreviewWindow(Book book)
        {
            InitializeComponent();
            _book = book;
            Title = $"// PREVIEW — {book.Title}";
            BuildPages();
            ShowPage(0);
        }

        private void BuildPages()
        {
            _pages.Clear();

            // ── Page 1: Cover ───────────────────────────────────────────
            var coverDoc = new FlowDocument { Background = Brushes.Transparent };
            coverDoc.Blocks.Add(MakeCoverPage(_book));
            _pages.Add(coverDoc);

            // ── Page 2: Title page ──────────────────────────────────────
            var titleDoc = new FlowDocument { Background = Brushes.Transparent };
            titleDoc.Blocks.Add(MakeTitlePage(_book));
            _pages.Add(titleDoc);

            // ── Page 3: Table of Contents ───────────────────────────────
            var tocDoc = new FlowDocument { Background = Brushes.Transparent };
            tocDoc.Blocks.Add(MakeTocPage(_book));
            _pages.Add(tocDoc);

            // ── Pages: Chapters ─────────────────────────────────────────
            foreach (var chapter in _book.Chapters.OrderBy(c => c.Order))
            {
                var chDoc = new FlowDocument { Background = Brushes.Transparent };

                // Chapter title
                var titlePara = new Paragraph(new Run(chapter.Title))
                {
                    FontSize     = 20,
                    FontWeight   = FontWeights.Bold,
                    Foreground   = new SolidColorBrush(Color.FromRgb(0, 255, 204)),
                    Margin       = new Thickness(0, 0, 0, 24),
                    TextAlignment = TextAlignment.Left
                };
                chDoc.Blocks.Add(titlePara);

                // Always build fresh FlowDocument from RTF bytes
                // (chapter.Document is owned by RichTextBox and may have empty Blocks)
                FlowDocument? freshDoc = null;
                if (!string.IsNullOrEmpty(chapter.RtfContent))
                {
                    try
                    {
                        var bytes = Convert.FromBase64String(chapter.RtfContent);
                        freshDoc = new FlowDocument();
                        using var ms = new System.IO.MemoryStream(bytes);
                        var range = new System.Windows.Documents.TextRange(
                            freshDoc.ContentStart, freshDoc.ContentEnd);
                        range.Load(ms, System.Windows.DataFormats.Rtf);
                    }
                    catch { freshDoc = null; }
                }

                if (freshDoc != null)
                {
                    foreach (var block in freshDoc.Blocks.ToList())
                    {
                        if (block is Paragraph p)
                        {
                            var copy = new Paragraph
                            {
                                FontSize      = 13,
                                Foreground    = new SolidColorBrush(Color.FromRgb(200, 230, 255)),
                                LineHeight     = 22,
                                Margin        = new Thickness(0, 0, 0, 8),
                                TextAlignment = TextAlignment.Justify
                            };
                            foreach (var inline in p.Inlines.ToList())
                            {
                                if (inline is Run r)
                                    copy.Inlines.Add(new Run(r.Text)
                                    {
                                        FontWeight = r.FontWeight,
                                        FontStyle  = r.FontStyle,
                                        Foreground = new SolidColorBrush(Color.FromRgb(200, 230, 255))
                                    });
                            }
                            chDoc.Blocks.Add(copy);
                        }
                    }
                }

                if (!chDoc.Blocks.Any())
                    chDoc.Blocks.Add(new Paragraph(new Run("[ Глава пуста ]"))
                    {
                        Foreground = new SolidColorBrush(Color.FromRgb(58, 96, 128)),
                        FontStyle  = FontStyles.Italic,
                        FontSize   = 12
                    });

                _pages.Add(chDoc);
            }

            // ── Last page: Back cover ───────────────────────────────────
            var backDoc = new FlowDocument { Background = Brushes.Transparent };
            backDoc.Blocks.Add(MakeBackCoverPage(_book));
            _pages.Add(backDoc);
        }

        private void ShowPage(int index)
        {
            if (index < 0 || index >= _pages.Count) return;
            _currentPage = index;
            PageViewer.Document = _pages[index];
            PageInfo.Text = $"Страница {index + 1} из {_pages.Count}";
            PrevBtn.IsEnabled = index > 0;
            NextBtn.IsEnabled = index < _pages.Count - 1;
            PrevBtn.Opacity = index > 0 ? 1 : 0.3;
            NextBtn.Opacity = index < _pages.Count - 1 ? 1 : 0.3;
        }

        // ── Page builders ─────────────────────────────────────────────

        private static Section MakeCoverPage(Book book)
        {
            var sec = new Section();

            var hasCover = book.Cover.GetFrontCoverBytes() != null;

            // Cover visual - gradient box if no image
            var coverPara = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 60, 0, 40) };

            if (hasCover)
            {
                try
                {
                    var bytes = book.Cover.GetFrontCoverBytes()!;
                    var img   = new System.Windows.Controls.Image
                    {
                        Width  = 300,
                        Height = 420,
                        Stretch = Stretch.Uniform
                    };
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.StreamSource    = new System.IO.MemoryStream(bytes);
                    bmp.CacheOption     = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    img.Source = bmp;
                    coverPara.Inlines.Add(new InlineUIContainer(img));
                }
                catch { hasCover = false; }
            }

            if (!hasCover)
            {
                var box = new System.Windows.Controls.Border
                {
                    Width = 280, Height = 400,
                    Background = new LinearGradientBrush(
                        Color.FromRgb(6, 78, 59),
                        Color.FromRgb(4, 6, 14),
                        90),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0, 255, 204)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4)
                };
                var sp = new System.Windows.Controls.StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(30)
                };
                sp.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = book.Title,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 22, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 204)),
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 16)
                });
                sp.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = book.Author,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(122, 168, 152)),
                    TextAlignment = TextAlignment.Center
                });
                box.Child = sp;
                coverPara.Inlines.Add(new InlineUIContainer(box));
            }

            sec.Blocks.Add(coverPara);
            return sec;
        }

        private static Section MakeTitlePage(Book book)
        {
            var sec = new Section();

            sec.Blocks.Add(new Paragraph(new Run(book.Author))
            {
                FontSize = 14, TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(122, 168, 152)),
                Margin = new Thickness(0, 120, 0, 16)
            });

            sec.Blocks.Add(new Paragraph(new Run(book.Title))
            {
                FontSize = 28, FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 204)),
                Margin = new Thickness(0, 0, 0, 24)
            });

            if (!string.IsNullOrWhiteSpace(book.Description))
                sec.Blocks.Add(new Paragraph(new Run(book.Description))
                {
                    FontSize = 12, FontStyle = FontStyles.Italic,
                    TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(122, 168, 152)),
                    Margin = new Thickness(60, 0, 60, 0)
                });

            if (!string.IsNullOrWhiteSpace(book.Publisher))
                sec.Blocks.Add(new Paragraph(new Run(book.Publisher))
                {
                    FontSize = 11, TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(58, 96, 128)),
                    Margin = new Thickness(0, 40, 0, 0)
                });

            sec.Blocks.Add(new Paragraph(new Run(book.CreatedDate.Year.ToString()))
            {
                FontSize = 11, TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(58, 96, 128))
            });

            return sec;
        }

        private static Section MakeTocPage(Book book)
        {
            var sec = new Section();

            sec.Blocks.Add(new Paragraph(new Run("ОГЛАВЛЕНИЕ"))
            {
                FontSize = 18, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 204)),
                Margin = new Thickness(0, 40, 0, 32),
                TextAlignment = TextAlignment.Left
            });

            int pageNum = 4; // start after cover+title+toc
            foreach (var ch in book.Chapters.OrderBy(c => c.Order))
            {
                var row = new Paragraph { Margin = new Thickness(0, 0, 0, 12) };

                // Chapter title
                row.Inlines.Add(new Run(ch.Title)
                {
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 230, 255)),
                    FontSize   = 12
                });

                // Dots
                row.Inlines.Add(new Run("  ····················  ")
                {
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 52, 72)),
                    FontSize   = 10
                });

                // Page number
                row.Inlines.Add(new Run(pageNum.ToString())
                {
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 204)),
                    FontSize   = 12
                });

                sec.Blocks.Add(row);
                pageNum++;
            }

            return sec;
        }

        private static Section MakeBackCoverPage(Book book)
        {
            var sec = new Section();

            var hasCover = book.Cover.GetBackCoverBytes() != null;
            var coverPara = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 40, 0, 30) };

            if (hasCover)
            {
                try
                {
                    var bytes = book.Cover.GetBackCoverBytes()!;
                    var img   = new System.Windows.Controls.Image { Width = 300, Height = 420, Stretch = Stretch.Uniform };
                    var bmp   = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.StreamSource = new System.IO.MemoryStream(bytes);
                    bmp.CacheOption  = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    img.Source = bmp;
                    coverPara.Inlines.Add(new InlineUIContainer(img));
                }
                catch { hasCover = false; }
            }

            if (!hasCover)
            {
                // Description block
                if (!string.IsNullOrWhiteSpace(book.Description))
                    sec.Blocks.Add(new Paragraph(new Run($"\"{book.Description}\""))
                    {
                        FontSize = 14, FontStyle = FontStyles.Italic,
                        TextAlignment = TextAlignment.Center,
                        Foreground = new SolidColorBrush(Color.FromRgb(122, 168, 152)),
                        Margin = new Thickness(40, 80, 40, 40)
                    });

                sec.Blocks.Add(new Paragraph(new Run(book.Title))
                {
                    FontSize = 16, FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 204))
                });

                sec.Blocks.Add(new Paragraph(new Run(book.Author))
                {
                    FontSize = 12, TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(122, 168, 152)),
                    Margin = new Thickness(0, 8, 0, 0)
                });
                return sec;
            }

            sec.Blocks.Add(coverPara);
            return sec;
        }

        private void PrevPage_Click(object s, RoutedEventArgs e) => ShowPage(_currentPage - 1);
        private void NextPage_Click(object s, RoutedEventArgs e) => ShowPage(_currentPage + 1);
        private void Close_Click(object s, RoutedEventArgs e)    => Close();
    }
}
