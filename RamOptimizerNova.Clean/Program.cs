using Avalonia;
using System;
using System.IO;

namespace RamOptimizerNova;

sealed class Program
{
    private static string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "RamOptimizerNova_Error.log"
    );

    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            Log("Application starting...");
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
            Log("Application exited normally");
        }
        catch (Exception ex)
        {
            Log($"FATAL ERROR: {ex.GetType().Name}: {ex.Message}");
            Log($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Log($"Inner exception: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        try
        {
            Log("Building Avalonia app...");
            var builder = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
            Log("Avalonia app builder created successfully");
            return builder;
        }
        catch (Exception ex)
        {
            Log($"ERROR in BuildAvaloniaApp: {ex.Message}");
            throw;
        }
    }

    private static void Log(string message)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            File.AppendAllText(LogFile, $"[{timestamp}] {message}\n");
        }
        catch
        {
            // Ignore logging errors
        }
    }
}
