using BookWriter.Data.Entities;
using BookWriter.Models;
using Microsoft.EntityFrameworkCore;

namespace BookWriter.Data
{
    /// <summary>
    /// Repository — single access point for all SQLite operations.
    /// Converts between EF entities and domain models in both directions.
    /// </summary>
    public class BookRepository
    {
        // ── Library queries ───────────────────────────────────────────────

        public List<BookEntity> GetAllBooks()
        {
            using var ctx = new BookDbContext();
            return ctx.Books
                      .Where(b => !b.IsDeleted)
                      .Include(b => b.BookTags).ThenInclude(bt => bt.Tag)
                      .OrderByDescending(b => b.UpdatedAt)
                      .ToList();
        }

        public BookEntity? GetBookById(Guid id)
        {
            using var ctx = new BookDbContext();
            return ctx.Books
                      .Where(b => b.Id == id && !b.IsDeleted)
                      .Include(b => b.Chapters.OrderBy(c => c.Order))
                      .Include(b => b.Cover)
                      .Include(b => b.BookTags).ThenInclude(bt => bt.Tag)
                      .FirstOrDefault();
        }

        // ── Upsert book (insert or update) ────────────────────────────────

        public void UpsertBook(Book book)
        {
            using var ctx = new BookDbContext();
            var entity = ctx.Books
                            .Include(b => b.Chapters)
                            .Include(b => b.Cover)
                            .FirstOrDefault(b => b.Id == book.Id);

            bool isNew = entity == null;
            entity ??= new BookEntity { Id = book.Id };

            // Map scalar fields
            entity.Title        = book.Title;
            entity.Author       = book.Author;
            entity.Description  = book.Description;
            entity.Language     = book.Language;
            entity.Publisher    = book.Publisher;
            entity.FilePath     = book.FilePath;
            entity.FontFamily   = book.Settings.FontFamily;
            entity.FontSize     = book.Settings.FontSize;
            entity.PageWidth    = book.Settings.PageWidth;
            entity.PageHeight   = book.Settings.PageHeight;
            entity.MarginTop    = book.Settings.MarginTop;
            entity.MarginBottom = book.Settings.MarginBottom;
            entity.MarginLeft   = book.Settings.MarginLeft;
            entity.MarginRight  = book.Settings.MarginRight;
            entity.IsDarkTheme  = book.Settings.IsDarkTheme;
            entity.UpdatedAt    = DateTime.UtcNow;

            if (isNew) ctx.Books.Add(entity);

            // Sync chapters
            var existingChapterIds = entity.Chapters.Select(c => c.Id).ToHashSet();
            var newChapterIds      = book.Chapters.Select(c => c.Id).ToHashSet();

            // Remove deleted chapters
            foreach (var removed in entity.Chapters.Where(c => !newChapterIds.Contains(c.Id)).ToList())
                ctx.Chapters.Remove(removed);

            foreach (var ch in book.Chapters)
            {
                ch.SaveDocumentToRtf();
                var chEntity = entity.Chapters.FirstOrDefault(c => c.Id == ch.Id);
                bool newChapter = chEntity == null;
                chEntity ??= new ChapterEntity { Id = ch.Id, BookId = entity.Id };
                chEntity.Title      = ch.Title;
                chEntity.Order      = ch.Order;
                chEntity.RtfContent = ch.RtfContent;
                chEntity.WordCount  = CountWords(ch.GetPlainText());
                chEntity.UpdatedAt  = DateTime.UtcNow;
                if (newChapter) ctx.Chapters.Add(chEntity);
            }

            // Sync cover
            var frontBytes = book.Cover.GetFrontCoverBytes();
            var backBytes  = book.Cover.GetBackCoverBytes();
            if (frontBytes != null || backBytes != null)
            {
                var cover = entity.Cover ?? new CoverEntity { BookId = entity.Id };
                if (frontBytes != null) cover.FrontImageBytes = frontBytes;
                if (backBytes  != null) cover.BackImageBytes  = backBytes;
                if (entity.Cover == null) ctx.Covers.Add(cover);
            }

            ctx.SaveChanges();
        }

        // ── Soft delete ────────────────────────────────────────────────────

        public void DeleteBook(Guid id)
        {
            using var ctx = new BookDbContext();
            var entity = ctx.Books.Find(id);
            if (entity == null) return;
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            ctx.SaveChanges();
        }

        // ── Convert entity → domain model ─────────────────────────────────

        public Book EntityToModel(BookEntity e)
        {
            var book = new Book
            {
                Id          = e.Id,
                Title       = e.Title,
                Author      = e.Author,
                Description = e.Description,
                Language    = e.Language,
                Publisher   = e.Publisher,
                FilePath    = e.FilePath,
                CreatedDate = e.CreatedAt.ToLocalTime(),
                ModifiedDate = e.UpdatedAt.ToLocalTime(),
                Settings = new BookSettings
                {
                    FontFamily   = e.FontFamily,
                    FontSize     = e.FontSize,
                    PageWidth    = e.PageWidth,
                    PageHeight   = e.PageHeight,
                    MarginTop    = e.MarginTop,
                    MarginBottom = e.MarginBottom,
                    MarginLeft   = e.MarginLeft,
                    MarginRight  = e.MarginRight,
                    IsDarkTheme  = e.IsDarkTheme,
                }
            };

            if (e.Cover != null)
            {
                if (e.Cover.FrontImageBytes != null)
                    book.Cover.FrontCoverBase64 = Convert.ToBase64String(e.Cover.FrontImageBytes);
                if (e.Cover.BackImageBytes != null)
                    book.Cover.BackCoverBase64 = Convert.ToBase64String(e.Cover.BackImageBytes);
            }

            foreach (var ch in e.Chapters.OrderBy(c => c.Order))
            {
                var chapter = new Chapter
                {
                    Id         = ch.Id,
                    Title      = ch.Title,
                    Order      = ch.Order,
                    RtfContent = ch.RtfContent
                };
                chapter.LoadDocumentFromRtf();
                book.Chapters.Add(chapter);
            }

            return book;
        }

        // ── Revisions ─────────────────────────────────────────────────────

        public void SaveRevision(Guid chapterId, string rtfSnapshot, string label = "")
        {
            using var ctx = new BookDbContext();
            ctx.Revisions.Add(new RevisionEntity
            {
                ChapterId   = chapterId,
                RtfSnapshot = rtfSnapshot,
                Label       = label,
                WordCount   = CountWords(rtfSnapshot),
                CreatedAt   = DateTime.UtcNow
            });
            ctx.SaveChanges();
        }

        public List<RevisionEntity> GetRevisions(Guid chapterId)
        {
            using var ctx = new BookDbContext();
            return ctx.Revisions
                      .Where(r => r.ChapterId == chapterId)
                      .OrderByDescending(r => r.CreatedAt)
                      .Take(50)
                      .ToList();
        }

        // ── Tags ──────────────────────────────────────────────────────────

        public List<TagEntity> GetAllTags()
        {
            using var ctx = new BookDbContext();
            return ctx.Tags.OrderBy(t => t.Name).ToList();
        }

        public void AddTagToBook(Guid bookId, string tagName, string color = "#00FFCC")
        {
            using var ctx = new BookDbContext();
            var tag = ctx.Tags.FirstOrDefault(t => t.Name == tagName)
                      ?? new TagEntity { Name = tagName, Color = color };
            if (tag.Id == 0) ctx.Tags.Add(tag);

            var exists = ctx.BookTags.Any(bt => bt.BookId == bookId && bt.Tag.Name == tagName);
            if (!exists)
            {
                ctx.SaveChanges(); // ensure tag has ID
                ctx.BookTags.Add(new BookTagEntity { BookId = bookId, TagId = tag.Id });
                ctx.SaveChanges();
            }
        }

        // ── Stats ─────────────────────────────────────────────────────────

        public (int books, int chapters, int words) GetLibraryStats()
        {
            using var ctx = new BookDbContext();
            var books    = ctx.Books.Count(b => !b.IsDeleted);
            var chapters = ctx.Chapters.Count();
            var words    = ctx.Chapters.Sum(c => (int?)c.WordCount) ?? 0;
            return (books, chapters, words);
        }

        // ── Helper ────────────────────────────────────────────────────────

        private static int CountWords(string text)
            => string.IsNullOrWhiteSpace(text)
                ? 0
                : text.Split(new[] { ' ', '\n', '\r', '\t' },
                             StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
