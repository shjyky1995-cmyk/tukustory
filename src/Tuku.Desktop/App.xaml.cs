namespace Tuku.Desktop
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Threading;

    public partial class App : Application
    {
        private string? logPath;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var smoke = false;
            foreach (var arg in e.Args)
            {
                if (string.Equals(arg, "--smoke", StringComparison.OrdinalIgnoreCase))
                {
                    smoke = true;
                }
            }

            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Tuku",
                "logs");
            Directory.CreateDirectory(logDirectory);
            logPath = Path.Combine(logDirectory, "startup.log");

            DispatcherUnhandledException += OnDispatcherUnhandledException;

            try
            {
                var mainWindow = new Views.MainWindow();
                mainWindow.Show();

                if (smoke)
                {
                    var timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(4)
                    };
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        Log("smoke: window shown and alive");
                        Shutdown(0);
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                Log("startup failed: " + ex);
                Shutdown(2);
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log("unhandled: " + e.Exception);
            e.Handled = true;
            Shutdown(2);
        }

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(logPath, DateTime.Now.ToString("o") + " " + message + Environment.NewLine);
            }
            catch
            {
                // 日志失败不影响启动流程结论
            }
        }
    }
}
