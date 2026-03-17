using BookWriter.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Windows.Documents;
using QColors = QuestPDF.Helpers.Colors;

namespace BookWriter.Services
{
    public class PdfExportService
    {
        public void Export(Book book, string outputPath)
        {
            // Set license before generating

            var s  = book.Settings;
            float pw = (float)s.PageWidth;
            float ph = (float)s.PageHeight;
            float mt = (float)s.MarginTop;
            float mb = (float)s.MarginBottom;
            float ml = (float)s.MarginLeft;
            float mr = (float)s.MarginRight;

            var doc = Document.Create(container =>
            {
                // Front cover
                var frontBytes = book.Cover.GetFrontCoverBytes();
                if (frontBytes != null)
                {
                    container.Page(page =>
                    {
                        page.Size(pw, ph, Unit.Millimetre);
                        page.Margin(0);
                        page.Content().Image(frontBytes).FitArea();
                    });
                }

                // Title page
                container.Page(page =>
                {
                    page.Size(pw, ph, Unit.Millimetre);
                    page.Margin(ml, Unit.Millimetre);
                    page.MarginRight(mr, Unit.Millimetre);
                    page.MarginTop(mt, Unit.Millimetre);
                    page.MarginBottom(mb, Unit.Millimetre);

                    page.Content().Column(col =>
                    {
                        col.Spacing(8);
                        col.Item().PaddingTop(40, Unit.Millimetre)
                            .AlignCenter()
                            .Text(book.Author)
                            .FontSize(14).FontColor(QColors.Grey.Darken2);

                        col.Item().AlignCenter()
                            .Text(book.Title)
                            .FontSize(24).Bold()
                            .FontColor(QColors.Black);

                        if (!string.IsNullOrWhiteSpace(book.Description))
                            col.Item().PaddingTop(20, Unit.Millimetre).AlignCenter()
                                .Text(book.Description)
                                .FontSize(11).Italic()
                                .FontColor(QColors.Grey.Darken1);
                    });
                });

                // Chapters
                foreach (var chapter in book.Chapters.OrderBy(c => c.Order))
                {
                    var paragraphs = ExtractParagraphs(chapter, s);
                    container.Page(page =>
                    {
                        page.Size(pw, ph, Unit.Millimetre);
                        page.Margin(ml, Unit.Millimetre);
                        page.MarginRight(mr, Unit.Millimetre);
                        page.MarginTop(mt, Unit.Millimetre);
                        page.MarginBottom(mb, Unit.Millimetre);

                        page.Header()
                            .BorderBottom(0.5f).BorderColor(QColors.Grey.Lighten1)
                            .PaddingBottom(4)
                            .Row(row =>
                            {
                                row.RelativeItem().Text(book.Title)
                                    .FontSize(8).FontColor(QColors.Grey.Medium);
                                row.ConstantItem(40).AlignRight()
                                    .Text(x => x.CurrentPageNumber().FontSize(8));
                            });

                        page.Content().Column(col =>
                        {
                            col.Item().PaddingBottom(10)
                                .Text(chapter.Title)
                                .FontSize((float)s.FontSize + 6).Bold()
                                .FontFamily(s.FontFamily);

                            foreach (var (text, bold, italic, fontSize, isHeading) in paragraphs)
                            {
                                col.Item().Text(t =>
                                {
                                    var span = t.Span(text);
                                    span.FontFamily(s.FontFamily);
                                    span.FontSize(isHeading ? (float)s.FontSize + 4 : (float)fontSize);
                                    if (bold || isHeading) span.Bold();
                                    if (italic) span.Italic();
                                });
                            }
                        });

                        page.Footer().AlignCenter()
                            .Text(x => x.CurrentPageNumber().FontSize(9));
                    });
                }

                // Back cover
                var backBytes = book.Cover.GetBackCoverBytes();
                if (backBytes != null)
                {
                    container.Page(page =>
                    {
                        page.Size(pw, ph, Unit.Millimetre);
                        page.Margin(0);
                        page.Content().Image(backBytes).FitArea();
                    });
                }
            });

            doc.GeneratePdf(outputPath);
        }

        private static List<(string text, bool bold, bool italic, double fontSize, bool isHeading)>
            ExtractParagraphs(Chapter chapter, BookSettings s)
        {
            var result = new List<(string, bool, bool, double, bool)>();
            if (string.IsNullOrEmpty(chapter.RtfContent))
                return result;

            // Use our RTF parser — handles \uN unicode escapes (Cyrillic) correctly.
            // WPF TextRange.Load fails on \ltrch-wrapped unicode RTF produced by RichTextBox.
            var paragraphs = RtfParser.ExtractParagraphs(chapter.RtfContent);
            foreach (var para in paragraphs)
                if (!string.IsNullOrWhiteSpace(para))
                    result.Add((para, false, false, s.FontSize, false));

            return result;
        }
    }
}
