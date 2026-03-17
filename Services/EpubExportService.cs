using BookWriter.Models;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace BookWriter.Services
{
    public class EpubExportService
    {
        public void Export(Book book, string outputPath)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"epub_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);
            try
            {
                BuildEpubStructure(book, tempDir);
                PackageAsZip(tempDir, outputPath);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        private void BuildEpubStructure(Book book, string dir)
        {
            File.WriteAllText(Path.Combine(dir, "mimetype"),
                "application/epub+zip", new UTF8Encoding(false));

            var metaDir = Path.Combine(dir, "META-INF");
            Directory.CreateDirectory(metaDir);
            File.WriteAllText(Path.Combine(metaDir, "container.xml"),
                ContainerXml(), new UTF8Encoding(false));

            var oebpsDir = Path.Combine(dir, "OEBPS");
            Directory.CreateDirectory(oebpsDir);
            Directory.CreateDirectory(Path.Combine(oebpsDir, "images"));
            Directory.CreateDirectory(Path.Combine(oebpsDir, "styles"));

            File.WriteAllText(Path.Combine(oebpsDir, "styles", "book.css"),
                BookCss(book.Settings), new UTF8Encoding(false));

            bool hasCover = false;
            var frontBytes = book.Cover.GetFrontCoverBytes();
            if (frontBytes != null)
            {
                File.WriteAllBytes(Path.Combine(oebpsDir, "images", "cover.jpg"), frontBytes);
                hasCover = true;
            }

            if (hasCover)
                File.WriteAllText(Path.Combine(oebpsDir, "cover.xhtml"),
                    CoverXhtml(book.Title), new UTF8Encoding(false));

            File.WriteAllText(Path.Combine(oebpsDir, "titlepage.xhtml"),
                TitlePageXhtml(book), new UTF8Encoding(false));

            var chapterFiles = new List<(string id, string fileName, string title)>();
            foreach (var ch in book.Chapters.OrderBy(c => c.Order))
            {
                var id = $"ch_{ch.Id:N}";
                var fileName = $"{id}.xhtml";
                chapterFiles.Add((id, fileName, ch.Title));
                File.WriteAllText(Path.Combine(oebpsDir, fileName),
                    ChapterXhtml(ch, book.Settings), new UTF8Encoding(false));
            }

            File.WriteAllText(Path.Combine(oebpsDir, "content.opf"),
                ContentOpf(book, chapterFiles, hasCover), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(oebpsDir, "toc.xhtml"),
                TocXhtml(book, chapterFiles), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(oebpsDir, "toc.ncx"),
                TocNcx(book, chapterFiles), new UTF8Encoding(false));
        }

        private void PackageAsZip(string sourceDir, string outputPath)
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
            using var zip = ZipFile.Open(outputPath, ZipArchiveMode.Create);

            var mimetypeEntry = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
            using (var sw = new StreamWriter(mimetypeEntry.Open(), new UTF8Encoding(false)))
                sw.Write("application/epub+zip");

            foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relPath = Path.GetRelativePath(sourceDir, file).Replace('\\', '/');
                if (relPath == "mimetype") continue;
                var entry = zip.CreateEntry(relPath, CompressionLevel.Optimal);
                using var src = File.OpenRead(file);
                using var dst = entry.Open();
                src.CopyTo(dst);
            }
        }

        private string ContainerXml() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">\n" +
            "  <rootfiles>\n" +
            "    <rootfile full-path=\"OEBPS/content.opf\" media-type=\"application/oebps-package+xml\"/>\n" +
            "  </rootfiles>\n" +
            "</container>";

        private string ContentOpf(Book book,
            List<(string id, string fileName, string title)> chapters, bool hasCover)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine($"<package xmlns=\"http://www.idpf.org/2007/opf\" version=\"3.0\" unique-identifier=\"uid\" xml:lang=\"{book.Language}\">");
            sb.AppendLine("  <metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\">");
            sb.AppendLine($"    <dc:identifier id=\"uid\">{book.Id}</dc:identifier>");
            sb.AppendLine($"    <dc:title>{Escape(book.Title)}</dc:title>");
            sb.AppendLine($"    <dc:creator>{Escape(book.Author)}</dc:creator>");
            sb.AppendLine($"    <dc:language>{book.Language}</dc:language>");
            sb.AppendLine($"    <dc:publisher>{Escape(book.Publisher)}</dc:publisher>");
            sb.AppendLine($"    <dc:date>{book.CreatedDate:yyyy-MM-dd}</dc:date>");
            if (!string.IsNullOrWhiteSpace(book.Description))
                sb.AppendLine($"    <dc:description>{Escape(book.Description)}</dc:description>");
            sb.AppendLine($"    <meta property=\"dcterms:modified\">{book.ModifiedDate:yyyy-MM-ddTHH:mm:ssZ}</meta>");
            if (hasCover) sb.AppendLine("    <meta name=\"cover\" content=\"cover-image\"/>");
            sb.AppendLine("  </metadata>");
            sb.AppendLine("  <manifest>");
            sb.AppendLine("    <item id=\"nav\" href=\"toc.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\"/>");
            sb.AppendLine("    <item id=\"ncx\" href=\"toc.ncx\" media-type=\"application/x-dtbncx+xml\"/>");
            sb.AppendLine("    <item id=\"css\" href=\"styles/book.css\" media-type=\"text/css\"/>");
            sb.AppendLine("    <item id=\"titlepage\" href=\"titlepage.xhtml\" media-type=\"application/xhtml+xml\"/>");
            if (hasCover)
            {
                sb.AppendLine("    <item id=\"cover-page\" href=\"cover.xhtml\" media-type=\"application/xhtml+xml\"/>");
                sb.AppendLine("    <item id=\"cover-image\" href=\"images/cover.jpg\" media-type=\"image/jpeg\" properties=\"cover-image\"/>");
            }
            foreach (var (id, fileName, _) in chapters)
                sb.AppendLine($"    <item id=\"{id}\" href=\"{fileName}\" media-type=\"application/xhtml+xml\"/>");
            sb.AppendLine("  </manifest>");
            sb.AppendLine("  <spine toc=\"ncx\">");
            if (hasCover) sb.AppendLine("    <itemref idref=\"cover-page\"/>");
            sb.AppendLine("    <itemref idref=\"titlepage\"/>");
            foreach (var (id, _, _) in chapters)
                sb.AppendLine($"    <itemref idref=\"{id}\"/>");
            sb.AppendLine("  </spine>");
            sb.AppendLine("</package>");
            return sb.ToString();
        }

        private string TocXhtml(Book book, List<(string id, string fileName, string title)> chapters)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\">");
            sb.AppendLine($"<head><title>Оглавление — {Escape(book.Title)}</title></head>");
            sb.AppendLine("<body>");
            sb.AppendLine("  <nav epub:type=\"toc\" id=\"toc\">");
            sb.AppendLine("    <h1>Оглавление</h1><ol>");
            sb.AppendLine("      <li><a href=\"titlepage.xhtml\">Титульная страница</a></li>");
            foreach (var (_, fileName, title) in chapters)
                sb.AppendLine($"      <li><a href=\"{fileName}\">{Escape(title)}</a></li>");
            sb.AppendLine("    </ol></nav></body></html>");
            return sb.ToString();
        }

        private string TocNcx(Book book, List<(string id, string fileName, string title)> chapters)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<ncx xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\">");
            sb.AppendLine($"  <head><meta name=\"dtb:uid\" content=\"{book.Id}\"/></head>");
            sb.AppendLine($"  <docTitle><text>{Escape(book.Title)}</text></docTitle>");
            sb.AppendLine("  <navMap>");
            int i = 1;
            sb.AppendLine($"    <navPoint id=\"np-0\" playOrder=\"{i++}\"><navLabel><text>Титульная страница</text></navLabel><content src=\"titlepage.xhtml\"/></navPoint>");
            foreach (var (id, fileName, title) in chapters)
                sb.AppendLine($"    <navPoint id=\"{id}\" playOrder=\"{i++}\"><navLabel><text>{Escape(title)}</text></navLabel><content src=\"{fileName}\"/></navPoint>");
            sb.AppendLine("  </navMap></ncx>");
            return sb.ToString();
        }

        private string CoverXhtml(string title)
        {
            var t = Escape(title);
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                   "<!DOCTYPE html>\n" +
                   "<html xmlns=\"http://www.w3.org/1999/xhtml\">\n" +
                   $"<head><title>{t}</title>\n" +
                   "<style>body{margin:0;padding:0;} img{width:100%;height:100vh;object-fit:contain;}</style>\n" +
                   "</head>\n" +
                   "<body epub:type=\"cover\" xmlns:epub=\"http://www.idpf.org/2007/ops\">\n" +
                   "  <img src=\"images/cover.jpg\" alt=\"Обложка\"/>\n" +
                   "</body></html>";
        }

        private string TitlePageXhtml(Book book)
        {
            var desc = string.IsNullOrEmpty(book.Description)
                ? ""
                : $"<p class=\"description\">{Escape(book.Description)}</p>";
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                   "<!DOCTYPE html>\n" +
                   "<html xmlns=\"http://www.w3.org/1999/xhtml\">\n" +
                   $"<head><title>{Escape(book.Title)}</title>\n" +
                   "<link rel=\"stylesheet\" type=\"text/css\" href=\"styles/book.css\"/>\n" +
                   "</head><body>\n" +
                   "  <div class=\"titlepage\">\n" +
                   $"    <h2 class=\"author\">{Escape(book.Author)}</h2>\n" +
                   $"    <h1 class=\"title\">{Escape(book.Title)}</h1>\n" +
                   $"    {desc}\n" +
                   $"    <p class=\"publisher\">{Escape(book.Publisher)}</p>\n" +
                   "  </div>\n" +
                   "</body></html>";
        }

        private string ChapterXhtml(Chapter chapter, BookSettings settings)
        {
            var bodyContent = new StringBuilder();

            // Use RtfParser — handles Cyrillic \uN unicode escapes correctly.
            // WPF TextRange.Load fails silently on \ltrch-wrapped unicode RTF.
            var rtfParagraphs = RtfParser.ExtractParagraphs(chapter.RtfContent);
            if (rtfParagraphs.Count > 0)
            {
                foreach (var para in rtfParagraphs)
                    if (!string.IsNullOrWhiteSpace(para))
                        bodyContent.AppendLine($"<p>{Escape(para)}</p>");
            }
            else
            {
                bodyContent.AppendLine("<p></p>");
            }

            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                   "<!DOCTYPE html>\n" +
                   "<html xmlns=\"http://www.w3.org/1999/xhtml\">\n" +
                   $"<head><title>{Escape(chapter.Title)}</title>\n" +
                   "<link rel=\"stylesheet\" type=\"text/css\" href=\"styles/book.css\"/>\n" +
                   "</head><body>\n" +
                   "  <section epub:type=\"chapter\" xmlns:epub=\"http://www.idpf.org/2007/ops\">\n" +
                   $"    <h2 class=\"chapter-title\">{Escape(chapter.Title)}</h2>\n" +
                   bodyContent.ToString() +
                   "  </section>\n" +
                   "</body></html>";
        }

        private string GetHtmlTag(Paragraph para)
        {
            if (para.Tag is string t)
                return t switch { "h1" => "h1", "h2" => "h2", "h3" => "h3", _ => "p" };
            return "p";
        }

        private string GetBlockText(ListItem item)
        {
            var sb = new StringBuilder();
            foreach (var block in item.Blocks)
                if (block is Paragraph p)
                    foreach (var inline in p.Inlines)
                        if (inline is Run r) sb.Append(Escape(r.Text));
            return sb.ToString();
        }

        private string BookCss(BookSettings s)
        {
            return $"body {{\n" +
                   $"    font-family: \"{s.FontFamily}\", Georgia, serif;\n" +
                   $"    font-size: {s.FontSize}pt;\n" +
                   $"    line-height: 1.6;\n" +
                   $"    margin: 0 auto;\n" +
                   $"    max-width: 600px;\n" +
                   $"    padding: 1em;\n" +
                   $"    color: #111;\n" +
                   $"}}\n" +
                   "h1, h2, h3 { font-weight: bold; line-height: 1.2; }\n" +
                   "h1.title { font-size: 2em; text-align: center; margin-top: 3em; }\n" +
                   "h2.author { font-size: 1.2em; text-align: center; color: #555; }\n" +
                   "h2.chapter-title { font-size: 1.5em; margin-bottom: 1em; }\n" +
                   "p { text-indent: 1.5em; margin: 0.3em 0; }\n" +
                   "p.description { font-style: italic; text-align: center; }\n" +
                   "div.titlepage { text-align: center; padding: 4em 2em; }\n" +
                   "img { max-width: 100%; }\n";
        }

        private static string Escape(string? s)
            => (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;")
                        .Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}
