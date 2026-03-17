using BookWriter.Models;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BookWriter.Views
{
    public partial class CoverEditorWindow : Window
    {
        private readonly Book _book;

        public CoverEditorWindow(Book book)
        {
            InitializeComponent();
            _book = book;

            if (!string.IsNullOrEmpty(book.Cover.FrontCoverBase64))
            {
                ShowImagePreview(FrontPreview, FrontLabel, book.Cover.GetFrontCoverBytes()!);
            }
            if (!string.IsNullOrEmpty(book.Cover.BackCoverBase64))
            {
                ShowImagePreview(BackPreview, BackLabel, book.Cover.GetBackCoverBytes()!);
            }
        }

        private void LoadFront_Click(object s, RoutedEventArgs e)
        {
            var path = PickImage();
            if (path == null) return;
            var bytes = File.ReadAllBytes(path);
            _book.Cover.FrontCoverBase64 = Convert.ToBase64String(bytes);
            _book.Cover.FrontCoverPath   = path;
            ShowImagePreview(FrontPreview, FrontLabel, bytes);
        }

        private void LoadBack_Click(object s, RoutedEventArgs e)
        {
            var path = PickImage();
            if (path == null) return;
            var bytes = File.ReadAllBytes(path);
            _book.Cover.BackCoverBase64 = Convert.ToBase64String(bytes);
            _book.Cover.BackCoverPath   = path;
            ShowImagePreview(BackPreview, BackLabel, bytes);
        }

        private static string? PickImage()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.webp",
                Title  = "Select cover image"
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        private static void ShowImagePreview(
            System.Windows.Controls.Border border,
            System.Windows.Controls.TextBlock label,
            byte[] bytes)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource    = new MemoryStream(bytes);
                bmp.CacheOption     = BitmapCacheOption.OnLoad;
                bmp.DecodePixelWidth = 220;
                bmp.EndInit();

                var img = new System.Windows.Controls.Image
                {
                    Source  = bmp,
                    Stretch = System.Windows.Media.Stretch.UniformToFill
                };
                border.Child = img;
            }
            catch { label.Text = "[ LOAD ERROR ]"; }
        }

        private void Save_Click(object s, RoutedEventArgs e)   => DialogResult = true;
        private void Cancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
    }
}
