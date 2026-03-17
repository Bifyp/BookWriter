using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace BookWriter.Models
{
    public class Chapter
    {
        public Guid   Id    { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "Новая глава";
        public int    Order { get; set; }

        /// <summary>RTF content stored as Base64 for JSON serialization.</summary>
        public string RtfContent { get; set; } = string.Empty;

        /// <summary>Notes / synopsis visible only to author.</summary>
        public string AuthorNotes { get; set; } = string.Empty;

        /// <summary>Status tag: Draft | Review | Final.</summary>
        public string Status { get; set; } = "Draft";

        public DateTime CreatedAt  { get; set; } = DateTime.Now;
        public DateTime UpdatedAt  { get; set; } = DateTime.Now;

        [JsonIgnore]
        public FlowDocument? Document { get; set; }

        [JsonIgnore]
        public int WordCount => CountWords(GetPlainText());

        [JsonIgnore]
        public bool IsModified { get; set; }

        // ── Text helpers ──────────────────────────────────────────────

        public string GetPlainText()
        {
            if (Document == null) return string.Empty; // Document not loaded yet — never return raw Base64
            var sb = new StringBuilder();
            foreach (var block in Document.Blocks)
            {
                if (block is Paragraph para)
                {
                    foreach (var inline in para.Inlines)
                        if (inline is Run run) sb.Append(run.Text);
                    sb.AppendLine();
                }
                else if (block is List list)
                {
                    foreach (var item in list.ListItems)
                        foreach (var b in item.Blocks)
                            if (b is Paragraph p)
                            {
                                foreach (var i in p.Inlines)
                                    if (i is Run r) sb.Append("• " + r.Text);
                                sb.AppendLine();
                            }
                }
                else if (block is BlockUIContainer bc)
                {
                    sb.AppendLine("[IMAGE]");
                }
            }
            return sb.ToString();
        }

        public string GetPreview(int maxChars = 160)
        {
            var text = GetPlainText().Trim();
            return text.Length <= maxChars ? text : text[..maxChars].TrimEnd() + "…";
        }

        // ── RTF serialization ─────────────────────────────────────────

        public void SaveDocumentToRtf()
        {
            if (Document == null) return;
            try
            {
                using var ms = new MemoryStream();
                var range = new TextRange(Document.ContentStart, Document.ContentEnd);
                range.Save(ms, DataFormats.Rtf);
                RtfContent  = Convert.ToBase64String(ms.ToArray());
                UpdatedAt   = DateTime.Now;
                IsModified  = false;
            }
            catch { /* best-effort */ }
        }

        public void LoadDocumentFromRtf()
        {
            Document = new FlowDocument();
            if (string.IsNullOrEmpty(RtfContent)) return;
            try
            {
                var bytes = Convert.FromBase64String(RtfContent);
                using var ms = new MemoryStream(bytes);
                var range = new TextRange(Document.ContentStart, Document.ContentEnd);
                range.Load(ms, DataFormats.Rtf);
            }
            catch
            {
                // Fallback: treat as plain text
                Document.Blocks.Add(new Paragraph(new Run(RtfContent)));
            }
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static int CountWords(string text) =>
            string.IsNullOrWhiteSpace(text) ? 0
            : text.Split(new[] { ' ', '\n', '\r', '\t' },
                         StringSplitOptions.RemoveEmptyEntries).Length;

        public override string ToString() => $"[{Order + 1}] {Title} ({WordCount}w)";
    }
}
