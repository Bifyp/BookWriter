using BookWriter.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace BookWriter.Data
{
    public class BookDbContext : DbContext
    {
        public static readonly string DbPath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BookWriter", "library.db");

        public DbSet<BookEntity>       Books       { get; set; } = null!;
        public DbSet<ChapterEntity>    Chapters    { get; set; } = null!;
        public DbSet<CoverEntity>      Covers      { get; set; } = null!;
        public DbSet<TagEntity>        Tags        { get; set; } = null!;
        public DbSet<BookTagEntity>    BookTags    { get; set; } = null!;
        public DbSet<RevisionEntity>   Revisions   { get; set; } = null!;
        public DbSet<AppSettingEntity> AppSettings { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder opt)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
            opt.UseSqlite($"Data Source={DbPath}");
        }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<BookEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).IsRequired().HasMaxLength(500);
                e.HasMany(x => x.Chapters).WithOne(c => c.Book)
                 .HasForeignKey(c => c.BookId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Cover).WithOne(c => c.Book)
                 .HasForeignKey<CoverEntity>(c => c.BookId).OnDelete(DeleteBehavior.Cascade);
            });

            mb.Entity<ChapterEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasMany(x => x.Revisions).WithOne(r => r.Chapter)
                 .HasForeignKey(r => r.ChapterId).OnDelete(DeleteBehavior.Cascade);
            });

            mb.Entity<CoverEntity>(e => e.HasKey(x => x.Id));

            mb.Entity<TagEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Name).IsUnique();
            });

            mb.Entity<BookTagEntity>(e =>
            {
                e.HasKey(x => new { x.BookId, x.TagId });
                e.HasOne(x => x.Book).WithMany(b => b.BookTags).HasForeignKey(x => x.BookId);
                e.HasOne(x => x.Tag).WithMany(t => t.BookTags).HasForeignKey(x => x.TagId);
            });

            mb.Entity<RevisionEntity>(e => e.HasKey(x => x.Id));

            mb.Entity<AppSettingEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Key).IsUnique();
            });

        }
    }
}
