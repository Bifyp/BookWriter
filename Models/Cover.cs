using System.IO;

namespace BookWriter.Models
{
    public class Cover
    {
        // File paths (for .bookproject saved locally)
        public string? FrontCoverPath { get; set; }
        public string? BackCoverPath  { get; set; }

        // Base64 blobs (embedded in JSON and SQLite)
        public string? FrontCoverBase64 { get; set; }
        public string? BackCoverBase64  { get; set; }

        public string FrontMimeType { get; set; } = "image/jpeg";
        public string BackMimeType  { get; set; } = "image/jpeg";

        // ── Accessors ─────────────────────────────────────────────────

        public byte[]? GetFrontCoverBytes()
        {
            if (!string.IsNullOrEmpty(FrontCoverBase64))
                return Convert.FromBase64String(FrontCoverBase64);
            if (!string.IsNullOrEmpty(FrontCoverPath) && File.Exists(FrontCoverPath))
                return File.ReadAllBytes(FrontCoverPath);
            return null;
        }

        public byte[]? GetBackCoverBytes()
        {
            if (!string.IsNullOrEmpty(BackCoverBase64))
                return Convert.FromBase64String(BackCoverBase64);
            if (!string.IsNullOrEmpty(BackCoverPath) && File.Exists(BackCoverPath))
                return File.ReadAllBytes(BackCoverPath);
            return null;
        }

        public bool HasFront => GetFrontCoverBytes() != null;
        public bool HasBack  => GetBackCoverBytes()  != null;

        /// <summary>Load image from disk and embed as Base64.</summary>
        public void LoadFrontFromFile(string path)
        {
            FrontCoverPath   = path;
            FrontCoverBase64 = Convert.ToBase64String(File.ReadAllBytes(path));
            FrontMimeType    = DetectMime(path);
        }

        public void LoadBackFromFile(string path)
        {
            BackCoverPath   = path;
            BackCoverBase64 = Convert.ToBase64String(File.ReadAllBytes(path));
            BackMimeType    = DetectMime(path);
        }

        public void ClearFront() { FrontCoverPath = null; FrontCoverBase64 = null; }
        public void ClearBack()  { BackCoverPath  = null; BackCoverBase64  = null; }

        private static string DetectMime(string path) =>
            Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".png"  => "image/png",
                ".webp" => "image/webp",
                ".bmp"  => "image/bmp",
                _       => "image/jpeg"
            };
    }
}
