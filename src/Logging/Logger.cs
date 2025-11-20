using System;
using System.IO;

namespace RamOptimizer.Logging
{
    public class Logger
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();

        public Logger(string logFilePath = "application.log")
        {
            _logFilePath = logFilePath;
        }

        public void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public void LogWarning(string message)
        {
            Log("WARNING", message);
        }

        public void LogError(string message)
        {
            Log("ERROR", message);
        }

        public void LogException(Exception ex)
        {
            Log("EXCEPTION", $"{ex.Message}\n{ex.StackTrace}");
        }

        private void Log(string level, string message)
        {
            try
            {
                lock (_lockObject)
                {
                    var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // If we can't log to file, output to console
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
            }
        }
    }
}