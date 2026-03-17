namespace BookWriter.Data.Entities
{
    public class BookEntity
    {
        public Guid   Id           { get; set; } = Guid.NewGuid();
        public string Title        { get; set; } = "Без названия";
        public string Author       { get; set; } = string.Empty;
        public string Description  { get; set; } = string.Empty;
        public string Language     { get; set; } = "ru";
        public string Publisher    { get; set; } = string.Empty;
        public string FontFamily   { get; set; } = "Consolas";
        public double FontSize     { get; set; } = 13;
        public double PageWidth    { get; set; } = 148;
        public double PageHeight   { get; set; } = 210;
        public double MarginTop    { get; set; } = 20;
        public double MarginBottom { get; set; } = 20;
        public double MarginLeft   { get; set; } = 25;
        public double MarginRight  { get; set; } = 15;
        public bool   IsDarkTheme  { get; set; } = true;
        public string? FilePath    { get; set; }
        public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt  { get; set; } = DateTime.UtcNow;
        public bool IsDeleted      { get; set; } = false;
        public ICollection<ChapterEntity> Chapters { get; set; } = new List<ChapterEntity>();
        public CoverEntity?               Cover    { get; set; }
        public ICollection<BookTagEntity> BookTags { get; set; } = new List<BookTagEntity>();
    }

    public class ChapterEntity
    {
        public Guid   Id         { get; set; } = Guid.NewGuid();
        public Guid   BookId     { get; set; }
        public string Title      { get; set; } = "Новая глава";
        public int    Order      { get; set; }
        public string RtfContent { get; set; } = string.Empty;
        public int    WordCount  { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public BookEntity                  Book      { get; set; } = null!;
        public ICollection<RevisionEntity> Revisions { get; set; } = new List<RevisionEntity>();
    }

    public class CoverEntity
    {
        public Guid   Id               { get; set; } = Guid.NewGuid();
        public Guid   BookId           { get; set; }
        public byte[]? FrontImageBytes { get; set; }
        public byte[]? BackImageBytes  { get; set; }
        public string  FrontMimeType   { get; set; } = "image/jpeg";
        public string  BackMimeType    { get; set; } = "image/jpeg";
        public BookEntity Book { get; set; } = null!;
    }

    public class TagEntity
    {
        public int    Id    { get; set; }
        public string Name  { get; set; } = string.Empty;
        public string Color { get; set; } = "#00FFCC";
        public ICollection<BookTagEntity> BookTags { get; set; } = new List<BookTagEntity>();
    }

    public class BookTagEntity
    {
        public Guid BookId { get; set; }
        public int  TagId  { get; set; }
        public BookEntity Book { get; set; } = null!;
        public TagEntity  Tag  { get; set; } = null!;
    }

    public class RevisionEntity
    {
        public Guid     Id          { get; set; } = Guid.NewGuid();
        public Guid     ChapterId   { get; set; }
        public string   RtfSnapshot { get; set; } = string.Empty;
        public int      WordCount   { get; set; }
        public string   Label       { get; set; } = string.Empty;
        public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
        public ChapterEntity Chapter { get; set; } = null!;
    }

    /// <summary>Key-value store — API keys stored DPAPI-encrypted.</summary>
    public class AppSettingEntity
    {
        public int    Id    { get; set; }
        public string Key   { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }


}
