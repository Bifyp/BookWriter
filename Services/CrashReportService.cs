using System.IO;
using BookWriter.Views;
using System.Reflection;
using System.Text;
using System.Windows;

namespace BookWriter.Services
{
    public static class CrashReportService
    {
        private static readonly string CrashDir =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BookWriter", "crashes");

        // Prevent recursive crash handling
        private static int _handling = 0;

        public static void Handle(Exception ex, string context = "")
        {
            if (System.Threading.Interlocked.Exchange(ref _handling, 1) == 1)
                return; // already handling a crash

            try
            {
                var report = BuildReport(ex, context);
                var path   = SaveReport(report);
                ShowDialog(ex, report, path);
            }
            catch (Exception inner)
            {
                // Absolute last resort — write to file and show plain MessageBox
                try
                {
                    var fallback = Path.Combine(CrashDir, $"crash_fallback_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                    Directory.CreateDirectory(CrashDir);
                    File.WriteAllText(fallback, $"Original: {ex}\n\nHandler error: {inner}");
                }
                catch { }

                try
                {
                    MessageBox.Show(
                        $"Критическая ошибка:\n{ex.Message}\n\nОтчёт сохранён в:\n{CrashDir}",
                        "BookWriter — Crash",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch { }
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref _handling, 0);
            }
        }

        private static string BuildReport(Exception ex, string context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════");
            sb.AppendLine("  BOOKWRITER CRASH REPORT");
            sb.AppendLine("═══════════════════════════════════════════════");
            sb.AppendLine($"  Time     : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  Version  : {Assembly.GetExecutingAssembly().GetName().Version}");
            sb.AppendLine($"  OS       : {Environment.OSVersion}");
            sb.AppendLine($"  .NET     : {Environment.Version}");
            sb.AppendLine($"  Context  : {context}");
            sb.AppendLine("───────────────────────────────────────────────");
            sb.AppendLine($"  Exception: {ex.GetType().FullName}");
            sb.AppendLine($"  Message  : {ex.Message}");
            sb.AppendLine("───────────────────────────────────────────────");
            sb.AppendLine("  Stack Trace:");
            sb.AppendLine(ex.StackTrace);

            var inner = ex.InnerException;
            int depth = 1;
            while (inner != null)
            {
                sb.AppendLine($"───── Inner Exception #{depth} ─────");
                sb.AppendLine($"  {inner.GetType().FullName}: {inner.Message}");
                sb.AppendLine(inner.StackTrace);
                inner = inner.InnerException;
                depth++;
            }

            sb.AppendLine("═══════════════════════════════════════════════");
            return sb.ToString();
        }

        private static string SaveReport(string report)
        {
            Directory.CreateDirectory(CrashDir);
            var fileName = $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var path     = Path.Combine(CrashDir, fileName);
            File.WriteAllText(path, report, Encoding.UTF8);

            var files = Directory.GetFiles(CrashDir, "crash_*.txt")
                                 .OrderByDescending(f => f)
                                 .Skip(20);
            foreach (var old in files)
                try { File.Delete(old); } catch { }

            return path;
        }

        private static void ShowDialog(Exception ex, string report, string reportPath)
        {
            var app = Application.Current;
            if (app == null)
            {
                // No UI available — just show a plain MessageBox
                MessageBox.Show(
                    $"Критическая ошибка: {ex.Message}\n\nОтчёт: {reportPath}",
                    "BookWriter — Crash",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Always marshal to UI thread
            app.Dispatcher.Invoke(() =>
            {
                try
                {
                    var dlg = new CrashReportWindow(ex.Message, report, reportPath);
                    dlg.ShowDialog();
                }
                catch
                {
                    MessageBox.Show(
                        $"Критическая ошибка: {ex.Message}\n\nОтчёт сохранён:\n{reportPath}",
                        "BookWriter — Crash",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            });
        }
    }
}
