using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RamOptimizerNova.Services.Interfaces;

namespace RamOptimizerNova.Services;

/// <summary>
/// Service that handles error handling and logging for the Nova UI
/// Provides centralized error management, logging, and user notifications
/// </summary>
public class ErrorHandlingAndLoggingService : IErrorHandlingAndLoggingService, IDisposable
{
    private readonly ILogger<ErrorHandlingAndLoggingService> _logger;
    private readonly string _logDirectory;
    private readonly string _logFilePath;
    private readonly int _maxLogFiles = 10;
    private readonly long _maxLogFileSize = 10 * 1024 * 1024; // 10MB
    private readonly int _maxErrorHistory = 100;
    private readonly int _maxWarningHistory = 200;
    private readonly int _maxInfoHistory = 500;

    private readonly ConcurrentQueue<LogEntry> _errorHistory = new();
    private readonly ConcurrentQueue<LogEntry> _warningHistory = new();
    private readonly ConcurrentQueue<LogEntry> _infoHistory = new();
    private readonly SemaphoreSlim _logLock = new(1, 1);
    private readonly SemaphoreSlim _historyLock = new(1, 1);

    private bool _isInitialized = false;
    private bool _isLoggingEnabled = true;
    private LogLevel _minimumLogLevel = LogLevel.Information;
    private LogFormat _logFormat = LogFormat.Text;
    private bool _autoRotateLogs = true;
    private bool _autoCleanupLogs = true;
    private int _cleanupIntervalHours = 24;

    // Event handlers
    public event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;
    public event EventHandler<WarningOccurredEventArgs>? WarningOccurred;
    public event EventHandler<InfoLoggedEventArgs>? InfoLogged;
    public event EventHandler<LogClearedEventArgs>? LogCleared;
    public event EventHandler<LogRotatedEventArgs>? LogRotated;

    public ErrorHandlingAndLoggingService(ILogger<ErrorHandlingAndLoggingService> logger)
    {
        _logger = logger;
        
        // Setup log directory
        _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RamOptimizer", "Logs", "NovaUI");
        _logFilePath = Path.Combine(_logDirectory, $"nova_ui_{DateTime.Now:yyyyMMdd}.log");

        // Ensure log directory exists
        Directory.CreateDirectory(_logDirectory);
    }

    /// <summary>
    /// Initialize the error handling and logging service
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing Error Handling and Logging Service...");

            // Initialize logging
            await InitializeLoggingAsync();

            // Load configuration
            await LoadConfigurationAsync();

            // Start cleanup task
            StartCleanupTask();

            _isInitialized = true;
            _logger.LogInformation("Error Handling and Logging Service initialized successfully");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Error Handling and Logging Service");
            return false;
        }
    }

    /// <summary>
    /// Cleanup the error handling and logging service
    /// </summary>
    public async Task CleanupAsync()
    {
        try
        {
            _logger.LogInformation("Cleaning up Error Handling and Logging Service...");

            // Stop cleanup task
            StopCleanupTask();

            // Flush logs
            await FlushLogsAsync();

            // Cleanup resources
            await CleanupResourcesAsync();

            _isInitialized = false;
            _logger.LogInformation("Error Handling and Logging Service cleaned up successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up Error Handling and Logging Service");
        }
    }

    /// <summary>
    /// Log an error message
    /// </summary>
    public async Task LogErrorAsync(string message, Exception? exception = null, string? category = null, Dictionary<string, object>? context = null)
    {
        try
        {
            if (!_isLoggingEnabled || _minimumLogLevel > LogLevel.Error)
                return;

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                LogLevel = LogLevel.Error,
                Message = message,
                Exception = exception,
                Category = category ?? "General",
                Context = context ?? new Dictionary<string, object>(),
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                ProcessId = Process.GetCurrentProcess().Id
            };

            // Log to file
            await WriteLogEntryAsync(logEntry);

            // Add to history
            await AddToHistoryAsync(_errorHistory, logEntry, _maxErrorHistory);

            // Notify subscribers
            await NotifyErrorOccurred(logEntry);

            // Log to console
            _logger.LogError(exception, "[{Category}] {Message}", logEntry.Category, logEntry.Message);
        }
        catch (Exception ex)
        {
            // Fallback logging
            Console.WriteLine($"Error in ErrorHandlingAndLoggingService: {ex.Message}");
            _logger.LogError(ex, "Error in ErrorHandlingAndLoggingService");
        }
    }

    /// <summary>
    /// Log a warning message
    /// </summary>
    public async Task LogWarningAsync(string message, Exception? exception = null, string? category = null, Dictionary<string, object>? context = null)
    {
        try
        {
            if (!_isLoggingEnabled || _minimumLogLevel > LogLevel.Warning)
                return;

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                LogLevel = LogLevel.Warning,
                Message = message,
                Exception = exception,
                Category = category ?? "General",
                Context = context ?? new Dictionary<string, object>(),
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                ProcessId = Process.GetCurrentProcess().Id
            };

            // Log to file
            await WriteLogEntryAsync(logEntry);

            // Add to history
            await AddToHistoryAsync(_warningHistory, logEntry, _maxWarningHistory);

            // Notify subscribers
            await NotifyWarningOccurred(logEntry);

            // Log to console
            _logger.LogWarning(exception, "[{Category}] {Message}", logEntry.Category, logEntry.Message);
        }
        catch (Exception ex)
        {
            // Fallback logging
            Console.WriteLine($"Error in ErrorHandlingAndLoggingService: {ex.Message}");
            _logger.LogError(ex, "Error in ErrorHandlingAndLoggingService");
        }
    }

    /// <summary>
    /// Log an info message
    /// </summary>
    public async Task LogInfoAsync(string message, Exception? exception = null, string? category = null, Dictionary<string, object>? context = null)
    {
        try
        {
            if (!_isLoggingEnabled || _minimumLogLevel > LogLevel.Information)
                return;

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                LogLevel = LogLevel.Information,
                Message = message,
                Exception = exception,
                Category = category ?? "General",
                Context = context ?? new Dictionary<string, object>(),
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                ProcessId = Process.GetCurrentProcess().Id
            };

            // Log to file
            await WriteLogEntryAsync(logEntry);

            // Add to history
            await AddToHistoryAsync(_infoHistory, logEntry, _maxInfoHistory);

            // Notify subscribers
            await NotifyInfoLogged(logEntry);

            // Log to console
            _logger.LogInformation(exception, "[{Category}] {Message}", logEntry.Category, logEntry.Message);
        }
        catch (Exception ex)
        {
            // Fallback logging
            Console.WriteLine($"Error in ErrorHandlingAndLoggingService: {ex.Message}");
            _logger.LogError(ex, "Error in ErrorHandlingAndLoggingService");
        }
    }

    /// <summary>
    /// Log a debug message
    /// </summary>
    public async Task LogDebugAsync(string message, Exception? exception = null, string? category = null, Dictionary<string, object>? context = null)
    {
        try
        {
            if (!_isLoggingEnabled || _minimumLogLevel > LogLevel.Debug)
                return;

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                LogLevel = LogLevel.Debug,
                Message = message,
                Exception = exception,
                Category = category ?? "General",
                Context = context ?? new Dictionary<string, object>(),
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                ProcessId = Process.GetCurrentProcess().Id
            };

            // Log to file
            await WriteLogEntryAsync(logEntry);

            // Log to console
            _logger.LogDebug(exception, "[{Category}] {Message}", logEntry.Category, logEntry.Message);
        }
        catch (Exception ex)
        {
            // Fallback logging
            Console.WriteLine($"Error in ErrorHandlingAndLoggingService: {ex.Message}");
            _logger.LogError(ex, "Error in ErrorHandlingAndLoggingService");
        }
    }

    /// <summary>
    /// Log a trace message
    /// </summary>
    public async Task LogTraceAsync(string message, Exception? exception = null, string? category = null, Dictionary<string, object>? context = null)
    {
        try
        {
            if (!_isLoggingEnabled || _minimumLogLevel > LogLevel.Trace)
                return;

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                LogLevel = LogLevel.Trace,
                Message = message,
                Exception = exception,
                Category = category ?? "General",
                Context = context ?? new Dictionary<string, object>(),
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                ProcessId = Process.GetCurrentProcess().Id
            };

            // Log to file
            await WriteLogEntryAsync(logEntry);

            // Log to console
            _logger.LogTrace(exception, "[{Category}] {Message}", logEntry.Category, logEntry.Message);
        }
        catch (Exception ex)
        {
            // Fallback logging
            Console.WriteLine($"Error in ErrorHandlingAndLoggingService: {ex.Message}");
            _logger.LogError(ex, "Error in ErrorHandlingAndLoggingService");
        }
    }

    /// <summary>
    /// Get error history
    /// </summary>
    public async Task<List<LogEntry>> GetErrorHistoryAsync(int count = 50)
    {
        try
        {
            await _historyLock.WaitAsync();

            var errors = _errorHistory.Reverse().Take(count).ToList();
            return errors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting error history");
            return new List<LogEntry>();
        }
        finally
        {
            _historyLock.Release();
        }
    }

    /// <summary>
    /// Get warning history
    /// </summary>
    public async Task<List<LogEntry>> GetWarningHistoryAsync(int count = 100)
    {
        try
        {
            await _historyLock.WaitAsync();

            var warnings = _warningHistory.Reverse().Take(count).ToList();
            return warnings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting warning history");
            return new List<LogEntry>();
        }
        finally
        {
            _historyLock.Release();
        }
    }

    /// <summary>
    /// Get info history
    /// </summary>
    public async Task<List<LogEntry>> GetInfoHistoryAsync(int count = 200)
    {
        try
        {
            await _historyLock.WaitAsync();

            var infos = _infoHistory.Reverse().Take(count).ToList();
            return infos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting info history");
            return new List<LogEntry>();
        }
        finally
        {
            _historyLock.Release();
        }
    }

    /// <summary>
    /// Get all history
    /// </summary>
    public async Task<List<LogEntry>> GetAllHistoryAsync(LogLevel? minimumLevel = null, string? category = null, int count = 500)
    {
        try
        {
            await _historyLock.WaitAsync();

            var allHistory = new List<LogEntry>();

            // Add errors
            if (!minimumLevel.HasValue || minimumLevel.Value <= LogLevel.Error)
            {
                allHistory.AddRange(_errorHistory.Reverse());
            }

            // Add warnings
            if (!minimumLevel.HasValue || minimumLevel.Value <= LogLevel.Warning)
            {
                allHistory.AddRange(_warningHistory.Reverse());
            }

            // Add infos
            if (!minimumLevel.HasValue || minimumLevel.Value <= LogLevel.Information)
            {
                allHistory.AddRange(_infoHistory.Reverse());
            }

            // Filter by category
            if (!string.IsNullOrEmpty(category))
            {
                allHistory = allHistory.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Sort by timestamp (newest first)
            allHistory = allHistory.OrderByDescending(e => e.Timestamp).Take(count).ToList();

            return allHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all history");
            return new List<LogEntry>();
        }
        finally
        {
            _historyLock.Release();
        }
    }

    /// <summary>
    /// Clear error history
    /// </summary>
    public async Task ClearErrorHistoryAsync()
    {
        try
        {
            await _historyLock.WaitAsync();

            _errorHistory.Clear();
            await NotifyLogCleared("ErrorHistory");

            _logger.LogInformation("Error history cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing error history");
        }
        finally
        {
            _historyLock.Release();
        }
    }

    /// <summary>
    /// Clear warning history
    /// </summary>
    public async Task ClearWarningHistoryAsync()
    {
        try
        {
            await _historyLock.WaitAsync();

            _warningHistory.Clear();
            await NotifyLogCleared("WarningHistory");

            _logger.LogInformation("Warning history cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing warning history");
        }
        finally
        {
            _historyLock.Release();
        }
    }

    /// <summary>
    /// Clear info history
    /// </summary>
    public async Task ClearInfoHistoryAsync()
    {
        try
        {
            await _historyLock.WaitAsync();

            _infoHistory.Clear();
            await NotifyLogCleared("InfoHistory");

            _logger.LogInformation("Info history cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing info history");
        }
        finally
        {
            _historyLock.Release();
        }
    }

    /// <summary>
    /// Clear all history
    /// </summary>
    public async Task ClearAllHistoryAsync()
    {
        try
        {
            await _historyLock.WaitAsync();

            _errorHistory.Clear();
            _warningHistory.Clear();
            _infoHistory.Clear();
            await NotifyLogCleared("AllHistory");

            _logger.LogInformation("All history cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all history");
        }
        finally
        {
            _historyLock.Release();
        }
    }

    /// <summary>
    /// Export logs to file
    /// </summary>
    public async Task<string> ExportLogsAsync(LogLevel? minimumLevel = null, string? category = null, LogFormat format = LogFormat.Text)
    {
        try
        {
            var history = await GetAllHistoryAsync(minimumLevel, category, int.MaxValue);
            
            switch (format)
            {
                case LogFormat.Text:
                    return ExportToText(history);
                case LogFormat.Json:
                    return ExportToJson(history);
                case LogFormat.Csv:
                    return ExportToCsv(history);
                default:
                    return ExportToText(history);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting logs");
            throw;
        }
    }

    /// <summary>
    /// Get log statistics
    /// </summary>
    public async Task<LogStatistics> GetLogStatisticsAsync()
    {
        try
        {
            await _historyLock.WaitAsync();

            var stats = new LogStatistics
            {
                TotalErrors = _errorHistory.Count,
                TotalWarnings = _warningHistory.Count,
                TotalInfos = _infoHistory.Count,
                TotalLogEntries = _errorHistory.Count + _warningHistory.Count + _infoHistory.Count,
                ErrorHistory = _errorHistory.Reverse().Take(10).ToList(),
                WarningHistory = _warningHistory.Reverse().Take(10).ToList(),
                InfoHistory = _infoHistory.Reverse().Take(10).ToList(),
                Categories = GetUniqueCategories(),
                LogDirectory = _logDirectory,
                CurrentLogFilePath = _logFilePath,
                IsLoggingEnabled = _isLoggingEnabled,
                MinimumLogLevel = _minimumLogLevel,
                LogFormat = _logFormat,
                AutoRotateLogs = _autoRotateLogs,
                AutoCleanupLogs = _autoCleanupLogs
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting log statistics");
            return new LogStatistics();
        }
        finally
        {
            _historyLock.Release();
        }
    }

    /// <summary>
    /// Set logging configuration
    /// </summary>
    public async Task SetLoggingConfigurationAsync(LoggingConfiguration configuration)
    {
        try
        {
            _isLoggingEnabled = configuration.IsEnabled;
            _minimumLogLevel = configuration.MinimumLogLevel;
            _logFormat = configuration.LogFormat;
            _autoRotateLogs = configuration.AutoRotateLogs;
            _autoCleanupLogs = configuration.AutoCleanupLogs;
            _cleanupIntervalHours = configuration.CleanupIntervalHours;

            _logger.LogInformation("Logging configuration updated: Enabled={Enabled}, Level={Level}, Format={Format}", 
                _isLoggingEnabled, _minimumLogLevel, _logFormat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting logging configuration");
            throw;
        }
    }

    /// <summary>
    /// Get logging configuration
    /// </summary>
    public LoggingConfiguration GetLoggingConfiguration()
    {
        return new LoggingConfiguration
        {
            IsEnabled = _isLoggingEnabled,
            MinimumLogLevel = _minimumLogLevel,
            LogFormat = _logFormat,
            AutoRotateLogs = _autoRotateLogs,
            AutoCleanupLogs = _autoCleanupLogs,
            CleanupIntervalHours = _cleanupIntervalHours
        };
    }

    private async Task InitializeLoggingAsync()
    {
        try
        {
            _logger.LogInformation("Initializing logging...");

            // Check if log file exists and needs rotation
            if (File.Exists(_logFilePath))
            {
                var fileInfo = new FileInfo(_logFilePath);
                if (fileInfo.Length > _maxLogFileSize)
                {
                    await RotateLogAsync();
                }
            }

            _logger.LogInformation("Logging initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing logging");
            throw;
        }
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            _logger.LogInformation("Loading configuration...");

            // Load configuration from file or use defaults
            // This would typically load from a configuration file
            // For now, use defaults

            _logger.LogInformation("Configuration loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration");
            throw;
        }
    }

    private async Task WriteLogEntryAsync(LogEntry entry)
    {
        try
        {
            await _logLock.WaitAsync();

            var logLine = FormatLogEntry(entry);
            
            await File.AppendAllTextAsync(_logFilePath, logLine + Environment.NewLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing log entry");
            throw;
        }
        finally
        {
            _logLock.Release();
        }
    }

    private string FormatLogEntry(LogEntry entry)
    {
        try
        {
            switch (_logFormat)
            {
                case LogFormat.Json:
                    return FormatLogEntryAsJson(entry);
                case LogFormat.Csv:
                    return FormatLogEntryAsCsv(entry);
                default:
                    return FormatLogEntryAsText(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting log entry");
            return $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] [{entry.Category}] ERROR: {ex.Message}";
        }
    }

    private string FormatLogEntryAsText(LogEntry entry)
    {
        var contextStr = entry.Context.Any() ? $" | Context: {string.Join(", ", entry.Context.Select(kvp => $"{kvp.Key}={kvp.Value}"))}" : "";
        var exceptionStr = entry.Exception != null ? $" | Exception: {entry.Exception.Message}" : "";
        
        return $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] [{entry.Category}] {entry.Message}{contextStr}{exceptionStr}";
    }

    private string FormatLogEntryAsJson(LogEntry entry)
    {
        var contextStr = entry.Context.Any() ? $", \"Context\": {{{string.Join(", ", entry.Context.Select(kvp => $"\"{kvp.Key}\": \"{kvp.Value}\""))}}}" : "";
        var exceptionStr = entry.Exception != null ? $", \"Exception\": \"{entry.Exception.Message}\"" : "";
        
        return $"{{\"Timestamp\": \"{entry.Timestamp:yyyy-MM-ddTHH:mm:ss.fff}\", \"LogLevel\": \"{entry.Level}\", \"Category\": \"{entry.Category}\", \"Message\": \"{entry.Message}\", \"ThreadId\": {entry.ThreadId}, \"ProcessId\": {entry.ProcessId}{contextStr}{exceptionStr}}}";
    }

    private string FormatLogEntryAsCsv(LogEntry entry)
    {
        var contextStr = entry.Context.Any() ? $"\"{string.Join("|", entry.Context.Select(kvp => $"{kvp.Key}={kvp.Value}\"))}" : "\"\"";
        var exceptionStr = entry.Exception != null ? $"\"{entry.Exception.Message}\"" : "\"\"";
        
        return $"\"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\",\"{entry.Level}\",\"{entry.Category}\",\"{entry.Message}\",{entry.ThreadId},{entry.ProcessId},{contextStr},{exceptionStr}";
    }

    private async Task AddToHistoryAsync(ConcurrentQueue<LogEntry> history, LogEntry entry, int maxCount)
    {
        try
        {
            await _historyLock.WaitAsync();

            history.Enqueue(entry);

            // Trim history if it exceeds max count
            while (history.Count > maxCount)
            {
                history.TryDequeue(out _);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to history");
        }
        finally
        {
            _historyLock.Release();
        }
    }

    private List<string> GetUniqueCategories()
    {
        try
        {
            var categories = new HashSet<string>();
            
            foreach (var entry in _errorHistory)
            {
                categories.Add(entry.Category);
            }
            
            foreach (var entry in _warningHistory)
            {
                categories.Add(entry.Category);
            }
            
            foreach (var entry in _infoHistory)
            {
                categories.Add(entry.Category);
            }
            
            return categories.OrderBy(c => c).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unique categories");
            return new List<string>();
        }
    }

    private void StartCleanupTask()
    {
        try
        {
            if (!_autoCleanupLogs)
                return;

            _ = Task.Run(async () =>
            {
                while (_isInitialized)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(_cleanupIntervalHours));
                        await CleanupOldLogsAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in cleanup task");
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting cleanup task");
        }
    }

    private void StopCleanupTask()
    {
        // Cleanup task will stop automatically when _isInitialized becomes false
    }

    private async Task CleanupOldLogsAsync()
    {
        try
        {
            _logger.LogInformation("Cleaning up old logs...");

            var files = Directory.GetFiles(_logDirectory, "nova_ui_*.log")
                .OrderBy(f => f)
                .ToList();

            // Keep only the most recent files
            while (files.Count > _maxLogFiles)
            {
                var oldestFile = files.First();
                File.Delete(oldestFile);
                files.Remove(oldestFile);
            }

            _logger.LogInformation("Old logs cleaned up successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old logs");
        }
    }

    private async Task RotateLogAsync()
    {
        try
        {
            _logger.LogInformation("Rotating log file...");

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var rotatedFilePath = Path.Combine(_logDirectory, $"nova_ui_{timestamp}.log");
            
            File.Move(_logFilePath, rotatedFilePath);
            
            await NotifyLogRotated(_logFilePath, rotatedFilePath);

            _logger.LogInformation("Log file rotated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating log file");
            throw;
        }
    }

    private async Task FlushLogsAsync()
    {
        try
        {
            _logger.LogInformation("Flushing logs...");

            // Ensure all pending log entries are written to disk
            await Task.Delay(100); // Small delay to ensure logs are written

            _logger.LogInformation("Logs flushed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing logs");
        }
    }

    private async Task CleanupResourcesAsync()
    {
        try
        {
            _logger.LogInformation("Cleaning up resources...");

            // Cleanup any remaining resources
            await FlushLogsAsync();

            _logger.LogInformation("Resources cleaned up successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up resources");
        }
    }

    private string ExportToText(List<LogEntry> entries)
    {
        try
        {
            var lines = new List<string>
            {
                "RAM Optimizer Nova - Log Export",
                $"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"Total Entries: {entries.Count}",
                "",
                "========================================",
                ""
            };

            foreach (var entry in entries)
            {
                lines.Add(FormatLogEntryAsText(entry));
            }

            return string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to text");
            throw;
        }
    }

    private string ExportToJson(List<LogEntry> entries)
    {
        try
        {
            var lines = new List<string>
            {
                "{",
                "  \"ExportDate\": \"" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "\",",
                "  \"TotalEntries\": " + entries.Count + ",",
                "  \"Entries\": ["
            };

            for (int i = 0; i < entries.Count; i++)
            {
                lines.Add("    " + FormatLogEntryAsJson(entries[i]));
                if (i < entries.Count - 1)
                {
                    lines[lines.Count - 1] += ",";
                }
            }

            lines.Add("  ]");
            lines.Add("}");

            return string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to JSON");
            throw;
        }
    }

    private string ExportToCsv(List<LogEntry> entries)
    {
        try
        {
            var lines = new List<string>
            {
                "Timestamp,LogLevel,Category,Message,ThreadId,ProcessId,Context,Exception"
            };

            foreach (var entry in entries)
            {
                lines.Add(FormatLogEntryAsCsv(entry));
            }

            return string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to CSV");
            throw;
        }
    }

    private async Task NotifyErrorOccurred(LogEntry entry)
    {
        try
        {
            ErrorOccurred?.Invoke(this, new ErrorOccurredEventArgs(entry));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying error occurred");
        }
    }

    private async Task NotifyWarningOccurred(LogEntry entry)
    {
        try
        {
            WarningOccurred?.Invoke(this, new WarningOccurredEventArgs(entry));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying warning occurred");
        }
    }

    private async Task NotifyInfoLogged(LogEntry entry)
    {
        try
        {
            InfoLogged?.Invoke(this, new InfoLoggedEventArgs(entry));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying info logged");
        }
    }

    private async Task NotifyLogCleared(string historyType)
    {
        try
        {
            LogCleared?.Invoke(this, new LogClearedEventArgs(historyType));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying log cleared");
        }
    }

    private async Task NotifyLogRotated(string oldFilePath, string newFilePath)
    {
        try
        {
            LogRotated?.Invoke(this, new LogRotatedEventArgs(oldFilePath, newFilePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying log rotated");
        }
    }

    public void Dispose()
    {
        try
        {
            CleanupAsync().Wait();
        }
        catch
        {
            // Ignore disposal errors
        }
    }
}

// Supporting classes and enums
public enum LogFormat
{
    Text,
    Json,
    Csv
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel LogLevel { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public string Category { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
    public int ThreadId { get; set; }
    public int ProcessId { get; set; }

    public override string ToString()
    {
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{LogLevel}] [{Category}] {Message}";
    }
}

public class LoggingConfiguration
{
    public bool IsEnabled { get; set; } = true;
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;
    public LogFormat LogFormat { get; set; } = LogFormat.Text;
    public bool AutoRotateLogs { get; set; } = true;
    public bool AutoCleanupLogs { get; set; } = true;
    public int CleanupIntervalHours { get; set; } = 24;
}

public class LogStatistics
{
    public int TotalErrors { get; set; }
    public int TotalWarnings { get; set; }
    public int TotalInfos { get; set; }
    public int TotalLogEntries { get; set; }
    public List<LogEntry> ErrorHistory { get; set; } = new();
    public List<LogEntry> WarningHistory { get; set; } = new();
    public List<LogEntry> InfoHistory { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string LogDirectory { get; set; } = string.Empty;
    public string CurrentLogFilePath { get; set; } = string.Empty;
    public bool IsLoggingEnabled { get; set; }
    public LogLevel MinimumLogLevel { get; set; }
    public LogFormat LogFormat { get; set; }
    public bool AutoRotateLogs { get; set; }
    public bool AutoCleanupLogs { get; set; }
}

// Event argument classes
public class ErrorOccurredEventArgs : EventArgs
{
    public LogEntry Entry { get; }

    public ErrorOccurredEventArgs(LogEntry entry)
    {
        Entry = entry;
    }
}

public class WarningOccurredEventArgs : EventArgs
{
    public LogEntry Entry { get; }

    public WarningOccurredEventArgs(LogEntry entry)
    {
        Entry = entry;
    }
}

public class InfoLoggedEventArgs : EventArgs
{
    public LogEntry Entry { get; }

    public InfoLoggedEventArgs(LogEntry entry)
    {
        Entry = entry;
    }
}

public class LogClearedEventArgs : EventArgs
{
    public string HistoryType { get; }

    public LogClearedEventArgs(string historyType)
    {
        HistoryType = historyType;
    }
}

public class LogRotatedEventArgs : EventArgs
{
    public string OldFilePath { get; }
    public string NewFilePath { get; }

    public LogRotatedEventArgs(string oldFilePath, string newFilePath)
    {
        OldFilePath = oldFilePath;
        NewFilePath = newFilePath;
    }
}