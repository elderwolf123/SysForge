using System;
using System.IO;

namespace RamOptimizerNova.Services;

public class FileLogger
{
    private static FileLogger? _instance;
    private static readonly object _lock = new object();
    private readonly string _logFilePath;
    private readonly StreamWriter _writer;

    private FileLogger()
    {
        // Create logs directory if it doesn't exist
        var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDir);

        // Create log file with timestamp
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _logFilePath = Path.Combine(logsDir, $"RamOptimizer_{timestamp}.log");

        // Open file for writing
        _writer = new StreamWriter(_logFilePath, append: true) { AutoFlush = true };

        Log("=== RamOptimizer Nova - Session Started ===");
        Log($"Log file: {_logFilePath}");
    }

    public static FileLogger Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new FileLogger();
                    }
                }
            }
            return _instance;
        }
    }

    public void Log(string message)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logLine = $"[{timestamp}] {message}";
            
            // Write to file
            _writer.WriteLine(logLine);
            
            // Also write to console
            Console.WriteLine(logLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR writing to log file: {ex.Message}");
        }
    }

    public void LogError(string message, Exception? ex = null)
    {
        Log($"❌ ERROR: {message}");
        if (ex != null)
        {
            Log($"   Exception: {ex.Message}");
            Log($"   Stack trace: {ex.StackTrace}");
        }
    }

    public void LogSuccess(string message)
    {
        Log($"✅ {message}");
    }

    public void LogWarning(string message)
    {
        Log($"⚠️  WARNING: {message}");
    }

    public void LogInfo(string message)
    {
        Log($"ℹ️  {message}");
    }

    public string GetLogFilePath() => _logFilePath;

    ~FileLogger()
    {
        try
        {
            Log("=== Session Ended ===");
            _writer?.Close();
            _writer?.Dispose();
        }
        catch { }
    }
}
