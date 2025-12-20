using System;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;

namespace RamOptimizer.Logging
{
    public class ComprehensiveLogger : IDisposable
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();
        private bool _disposed = false;
        private LogLevel _minimumLogLevel;
        private readonly ConcurrentQueue<LogEntry> _logQueue;
        private readonly Timer _flushTimer;
        private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(5);
        private bool _flushInProgress = false;

        public event EventHandler<LogEntry> LogEntryAdded;

        public ComprehensiveLogger(string logFilePath = "logs/ram_optimizer.log", LogLevel minimumLogLevel = LogLevel.Info)
        {
            _logFilePath = logFilePath;
            _minimumLogLevel = minimumLogLevel;
            _logQueue = new ConcurrentQueue<LogEntry>();

            // Ensure log directory exists
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Initialize flush timer
            _flushTimer = new Timer(FlushLogs, null, _flushInterval, _flushInterval);

            // Write startup log entry
            LogInfo("ComprehensiveLogger initialized");
        }

        public void Log(LogLevel level, string message, Exception exception = null)
        {
            // Check if log level is sufficient
            if (level < _minimumLogLevel)
                return;

            try
            {
                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = level,
                    Message = message,
                    Exception = exception
                };

                // Add to queue for batch processing
                _logQueue.Enqueue(logEntry);

                // Raise event immediately for real-time updates
                LogEntryAdded?.Invoke(this, logEntry);
            }
            catch (Exception ex)
            {
                // If we can't log to file, output to console as a last resort
                Console.WriteLine($"Failed to queue log entry: {ex.Message}");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
                if (exception != null)
                {
                    Console.WriteLine($"Exception: {exception}");
                }
            }
        }

        public void LogInfo(string message)
        {
            Log(LogLevel.Info, message);
        }

        public void LogWarning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public void LogError(string message)
        {
            Log(LogLevel.Error, message);
        }

        public void LogException(Exception ex, string message = null)
        {
            Log(LogLevel.Error, message ?? ex.Message, ex);
        }

        public void LogDebug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        private void FlushLogs(object state)
        {
            // Prevent multiple concurrent flush operations
            if (_flushInProgress)
                return;

            lock (_lockObject)
            {
                if (_flushInProgress || _logQueue.IsEmpty)
                    return;

                _flushInProgress = true;
            }

            try
            {
                // Process all queued log entries
                while (_logQueue.TryDequeue(out LogEntry entry))
                {
                    WriteToFile(entry);
                }
            }
            catch (Exception ex)
            {
                // If we can't write to file, output to console as a last resort
                Console.WriteLine($"Failed to flush log entries: {ex.Message}");
            }
            finally
            {
                _flushInProgress = false;
            }
        }

        private void WriteToFile(LogEntry entry)
        {
            try
            {
                var logLine = FormatLogEntry(entry);
                File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
            }
            catch (Exception)
            {
                // Silently ignore file write errors to prevent infinite recursion
            }
        }

        private string FormatLogEntry(LogEntry entry)
        {
            var formatted = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] {entry.Message}";

            if (entry.Exception != null)
            {
                formatted += Environment.NewLine + $"Exception: {entry.Exception.Message}";
                if (!string.IsNullOrEmpty(entry.Exception.StackTrace))
                {
                    formatted += Environment.NewLine + $"Stack Trace: {entry.Exception.StackTrace}";
                }
            }

            return formatted;
        }

        public void ClearLog()
        {
            lock (_lockObject)
            {
                try
                {
                    // Flush any pending entries first
                    FlushLogs(null);

                    if (File.Exists(_logFilePath))
                    {
                        File.WriteAllText(_logFilePath, string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to clear log file: {ex.Message}");
                }
            }
        }

        public string[] GetLogContents()
        {
            lock (_lockObject)
            {
                try
                {
                    // Flush any pending entries first
                    FlushLogs(null);

                    if (File.Exists(_logFilePath))
                    {
                        return File.ReadAllLines(_logFilePath);
                    }
                    return new string[0];
                }
                catch (Exception ex)
                {
                    LogError($"Failed to read log file: {ex.Message}");
                    return new string[0];
                }
            }
        }

        public void SetMinimumLogLevel(LogLevel level)
        {
            _minimumLogLevel = level;
            LogInfo($"Minimum log level set to {level}");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Flush any remaining log entries
                    FlushLogs(null);

                    // Dispose timer
                    _flushTimer?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}