using BookWriter.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;

namespace BookWriter.Data
{
    /// <summary>
    /// Runs on application startup.
    /// Ensures all SQLite tables exist (EnsureCreated).
    /// Optionally seeds default data.
    /// </summary>
    public static class DbMigrator
    {
        public static void Initialize()
        {
            using var ctx = new BookDbContext();
            ctx.Database.EnsureCreated();
            SeedDefaults(ctx);
        }

        private static void SeedDefaults(BookDbContext ctx)
        {
            // Seed default tags if none exist
            if (!ctx.Tags.Any())
            {
                ctx.Tags.AddRange(
                    new TagEntity { Name = "Фантастика",   Color = "#00FFCC" },
                    new TagEntity { Name = "Фэнтези",      Color = "#BF00FF" },
                    new TagEntity { Name = "Детектив",     Color = "#FF007A" },
                    new TagEntity { Name = "Роман",        Color = "#00AAFF" },
                    new TagEntity { Name = "Научпоп",      Color = "#FFE600" },
                    new TagEntity { Name = "Автобиография",Color = "#FF6600" },
                    new TagEntity { Name = "Черновик",     Color = "#3A6080" }
                );
                ctx.SaveChanges();
            }
        }

        /// <summary>Returns the SQLite DB file size in MB.</summary>
        public static string GetDbSizeInfo()
        {
            try
            {
                var info = new System.IO.FileInfo(BookDbContext.DbPath);
                if (!info.Exists) return "0 KB";
                var kb = info.Length / 1024.0;
                return kb > 1024
                    ? $"{kb / 1024:F1} MB"
                    : $"{kb:F0} KB";
            }
            catch { return "?"; }
        }
    }
}
