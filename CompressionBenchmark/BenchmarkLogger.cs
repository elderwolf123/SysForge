using System;
using System.IO;
using System.Text;

namespace CompressionBenchmark;

/// <summary>
/// Comprehensive logging system for tracking errors, compression issues, and stability problems.
/// </summary>
public class BenchmarkLogger
{
    private static readonly string LogPath = "compression_benchmark.log";
    private static readonly object _lock = new object();

    public static void LogInfo(string message)
    {
        WriteLog("INFO", message);
    }

    public static void LogWarning(string message)
    {
        WriteLog("WARN", message);
    }

    public static void LogError(string message, Exception? ex = null)
    {
        var fullMessage = ex != null ? $"{message}\nException: {ex.Message}\nStack: {ex.StackTrace}" : message;
        WriteLog("ERROR", fullMessage);
    }

    public static void LogCompressionIssue(string filename, string algorithm, string issue)
    {
        WriteLog("COMPRESSION_ISSUE", $"File: {filename}, Algorithm: {algorithm}, Issue: {issue}");
    }

    public static void LogDecompressionFailure(string filename, string algorithm, Exception ex)
    {
        WriteLog("DECOMPRESSION_FAIL", $"File: {filename}, Algorithm: {algorithm}, Error: {ex.Message}");
    }

    public static void LogProcessKillFailure(string processName, Exception ex)
    {
        WriteLog("PROCESS_KILL_FAIL", $"Process: {processName}, Error: {ex.Message}");
    }

    public static void LogCrash(string context, Exception ex)
    {
        WriteLog("CRASH", $"Context: {context}\nException: {ex.Message}\nStack: {ex.StackTrace}");
    }

    public static void LogFileSafety(string action, string filepath)
    {
        WriteLog("FILE_SAFETY", $"Action: {action}, File: {filepath}");
    }

    private static void WriteLog(string level, string message)
    {
        lock (_lock)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] [{level}] {message}\n";
                
                File.AppendAllText(LogPath, logEntry);
                
                // Also output to console if it's an error
                if (level == "ERROR" || level == "CRASH")
                {
                    Console.WriteLine($"❌ {message}");
                }
            }
            catch
            {
                // Fail silently - don't crash if logging fails
            }
        }
    }

    public static void LogSessionStart()
    {
        WriteLog("SESSION", "=== NEW BENCHMARK SESSION STARTED ===");
    }

    public static void LogSessionEnd()
    {
        WriteLog("SESSION", "=== BENCHMARK SESSION COMPLETED ===");
    }
}
