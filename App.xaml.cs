using BookWriter.Data;
using BookWriter.Services;
using QuestPDF.Infrastructure;
using System.Windows;
using System.Windows.Threading;

namespace BookWriter
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;

            // Global crash handlers
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                var exception = ex.ExceptionObject as Exception
                    ?? new Exception(ex.ExceptionObject?.ToString() ?? "Unknown error");
                // Save report to disk immediately (UI may be dead)
                try
                {
                    var dir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "BookWriter", "crashes");
                    System.IO.Directory.CreateDirectory(dir);
                    var path = System.IO.Path.Combine(dir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}_fatal.txt");
                    System.IO.File.WriteAllText(path, exception.ToString());
                }
                catch { }
                CrashReportService.Handle(exception, "AppDomain.UnhandledException");
            };

            DispatcherUnhandledException += (s, ex) =>
            {
                ex.Handled = true;
                CrashReportService.Handle(ex.Exception, "Dispatcher.UnhandledException");
            };

            TaskScheduler.UnobservedTaskException += (s, ex) =>
            {
                ex.SetObserved();
                CrashReportService.Handle(ex.Exception, "TaskScheduler.UnobservedTaskException");
            };

            // Init SQLite
            try { DbMigrator.Initialize(); }
            catch (Exception ex)
            {
                CrashReportService.Handle(ex, "DbMigrator.Initialize");
            }
        }
    }
}
