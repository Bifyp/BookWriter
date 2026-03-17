using System.IO;
using System.Windows;

namespace BookWriter.Views
{
    public partial class CrashReportWindow : Window
    {
        private readonly string _report;
        private readonly string _reportPath;

        public CrashReportWindow(string message, string report, string reportPath)
        {
            InitializeComponent();
            _report     = report;
            _reportPath = reportPath;

            ErrorMessage.Text    = message;
            StackTraceBox.Text   = report;
            ReportPathText.Text  = $"Сохранено: {reportPath}";
            ReportPathText.ToolTip = reportPath;
        }

        private void Copy_Click(object s, RoutedEventArgs e)
        {
            Clipboard.SetText(_report);
            MessageBox.Show("Скопировано в буфер обмена.", "OK",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenFolder_Click(object s, RoutedEventArgs e)
        {
            try
            {
                var dir = Path.GetDirectoryName(_reportPath)!;
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(dir)
                    { UseShellExecute = true });
            }
            catch { }
        }

        private void Close_Click(object s, RoutedEventArgs e) => Close();
    }
}
