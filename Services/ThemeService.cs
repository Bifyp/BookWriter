using System.Windows;

namespace BookWriter.Services
{
    /// <summary>
    /// Switches between Cyberpunk Cyan (LightTheme) and Cyberpunk Pink (DarkTheme)
    /// by swapping ResourceDictionaries at runtime.
    /// </summary>
    public class ThemeService
    {
        private bool   _isDark;
        private string _variant = "Cyan";   // Cyan | Pink

        public bool   IsDarkTheme => _isDark;
        public string Variant     => _variant;

        public event EventHandler? ThemeChanged;

        // ── Apply ─────────────────────────────────────────────────────

        public void Apply(bool dark, string variant = "")
        {
            _isDark   = dark;
            _variant  = string.IsNullOrEmpty(variant)
                        ? (dark ? "Pink" : "Cyan")
                        : variant;

            SwapThemeDictionary();
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Toggle() => Apply(!_isDark);

        public void ApplyCyan() => Apply(false, "Cyan");
        public void ApplyPink() => Apply(true,  "Pink");

        // ── Internal ─────────────────────────────────────────────────

        private void SwapThemeDictionary()
        {
            if (Application.Current == null) return;
            var merged = Application.Current.Resources.MergedDictionaries;

            // Remove existing theme dict
            var old = merged.FirstOrDefault(d =>
                d.Source?.OriginalString?.Contains("Theme") == true);
            if (old != null) merged.Remove(old);

            // Pick new source
            var src = _isDark
                ? "Themes/DarkTheme.xaml"
                : "Themes/LightTheme.xaml";

            merged.Insert(0, new ResourceDictionary
            {
                Source = new Uri(src, UriKind.Relative)
            });
        }
    }
}
