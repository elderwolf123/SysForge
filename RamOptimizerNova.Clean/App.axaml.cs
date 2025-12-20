using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RamOptimizerNova.Views;
using System;
using System.IO;

namespace RamOptimizerNova;

public partial class App : Application
{
    private static string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "RamOptimizerNova_Error.log"
    );

    public override void Initialize()
    {
        try
        {
            Log("App.Initialize() starting...");
            AvaloniaXamlLoader.Load(this);
            Log("XAML loaded successfully");
        }
        catch (Exception ex)
        {
            Log($"ERROR in Initialize: {ex.GetType().Name}: {ex.Message}");
            Log($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            Log("OnFrameworkInitializationCompleted starting...");
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Log("Creating TestWindow...");
                var window = new TestWindow();
                Log($"TestWindow created. Title: {window.Title}");
                
                desktop.MainWindow = window;
                Log($"MainWindow set. Will show window now.");
                
                window.Show();
                Log("Window.Show() called");
            }
            else
            {
                Log($"ApplicationLifetime type: {ApplicationLifetime?.GetType().Name ?? "null"}");
            }

            base.OnFrameworkInitializationCompleted();
            Log("OnFrameworkInitializationCompleted completed");
        }
        catch (Exception ex)
        {
            Log($"ERROR in OnFrameworkInitializationCompleted: {ex.GetType().Name}: {ex.Message}");
            Log($"Stack trace: {ex.StackTrace}");
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