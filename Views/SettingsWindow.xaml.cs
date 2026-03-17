using BookWriter.Models;
using BookWriter.Services;
using System.Windows;

namespace BookWriter.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly BookSettings _settings;
        private readonly ThemeService _theme;

        public SettingsWindow(BookSettings settings, ThemeService theme)
        {
            InitializeComponent();
            _settings = settings;
            _theme = theme;

            FontFamilyBox.Text   = settings.FontFamily;
            FontSizeBox.Text     = settings.FontSize.ToString();
            PageWidthBox.Text    = settings.PageWidth.ToString();
            PageHeightBox.Text   = settings.PageHeight.ToString();
            MarginTopBox.Text    = settings.MarginTop.ToString();
            MarginBottomBox.Text = settings.MarginBottom.ToString();
            MarginLeftBox.Text   = settings.MarginLeft.ToString();
            MarginRightBox.Text  = settings.MarginRight.ToString();
            DarkThemeCheck.IsChecked = settings.IsDarkTheme;
        }

        private void Apply_Click(object s, RoutedEventArgs e)
        {
            _settings.FontFamily   = FontFamilyBox.Text.Trim();
            if (double.TryParse(FontSizeBox.Text, out var fs))   _settings.FontSize   = fs;
            if (double.TryParse(PageWidthBox.Text,  out var pw)) _settings.PageWidth  = pw;
            if (double.TryParse(PageHeightBox.Text, out var ph)) _settings.PageHeight = ph;
            if (double.TryParse(MarginTopBox.Text,  out var mt)) _settings.MarginTop  = mt;
            if (double.TryParse(MarginBottomBox.Text,out var mb))_settings.MarginBottom=mb;
            if (double.TryParse(MarginLeftBox.Text, out var ml)) _settings.MarginLeft = ml;
            if (double.TryParse(MarginRightBox.Text,out var mr)) _settings.MarginRight= mr;
            _settings.IsDarkTheme = DarkThemeCheck.IsChecked == true;
            _theme.Apply(_settings.IsDarkTheme);
            DialogResult = true;
        }

        private void Cancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
    }
}
