using System.IO;
using System.Windows;

namespace GammaTool;

public partial class App : Application
{
    Mutex? _mutex;
    TrayApp? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Только один экземпляр
        _mutex = new Mutex(true, @"Local\GammaTool.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            Shutdown();
            return;
        }

        // У WinExe нет консоли, поэтому необработанные ошибки пишем в файл
        DispatcherUnhandledException += (_, args) => LogError(args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) => LogError(args.ExceptionObject as Exception);

        _tray = new TrayApp();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.Dispose();
        base.OnExit(e);
    }

    static void LogError(Exception? ex)
    {
        try
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GammaTool");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "error.log"), $"{DateTime.Now:s}  {ex}\n\n");
        }
        catch { }
    }
}
