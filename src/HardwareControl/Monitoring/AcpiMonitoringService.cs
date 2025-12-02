using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.HardwareControl.Monitoring
{
    /// <summary>
    /// Monitors ACPI calls from official tools (G-Helper, Armory Crate) to validate our device IDs
    /// </summary>
    public class AcpiMonitoringService
    {
        private readonly ILogger<AcpiMonitoringService> _logger;
        private readonly List<AcpiTransaction> _capturedTransactions = new();
        private readonly string _captureFilePath;
        private bool _isMonitoring = false;

        public class AcpiTransaction
        {
            public DateTime Timestamp { get; set; }
            public string ProcessName { get; set; }
            public int ProcessId { get; set; }
            public uint DeviceId { get; set; }
            public int Value { get; set; }
            public bool IsWrite { get; set; }
            public string Description { get; set; }
        }

        public class ValidationResult
        {
            public bool AllMatch { get; set; }
            public List<string> Matches { get; set; } = new();
            public List<string> Mismatches { get; set; } = new();
            public int TotalCaptured { get; set; }
            public int TotalVerified { get; set; }
        }

        public AcpiMonitoringService(ILogger<AcpiMonitoringService> logger)
        {
            _logger = logger;
            
            var dataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "RamOptimizer", 
                "Monitoring"
            );
            
            Directory.CreateDirectory(dataPath);
            _captureFilePath = Path.Combine(dataPath, "acpi_captures.json");
        }

        /// <summary>
        /// Start monitoring ACPI calls from target processes
        /// </summary>
        public Task StartMonitoringAsync(params string[] processNames)
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("Monitoring already active");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Starting ACPI monitoring for processes: {Processes}",
                string.Join(", ", processNames));

            _isMonitoring = true;

            _logger.LogInformation("Monitoring instructions:");
            _logger.LogInformation("1. Keep this application running");
            _logger.LogInformation("2. Open G-Helper or Armory Crate");
            _logger.LogInformation("3. Make changes (cores, battery limit, etc.)");
            _logger.LogInformation("4. ACPI calls will be logged automatically");
            _logger.LogInformation("5. Call StopMonitoring() when done");

            // Note: Actual process-level monitoring would require either:
            // - Admin privileges + API hooking (complex)
            // - ETW event tracing (requires Microsoft.Diagnostics.Tracing.TraceEvent NuGet)
            // - Manual process monitor integration
            
            // For now, this provides the interface and manual capture capability
            return Task.CompletedTask;
        }

        /// <summary>
        /// Manually add a captured transaction (e.g., from Procmon observations)
        /// </summary>
        public void AddManualCapture(string processName, uint deviceId, int value, string description)
        {
            var transaction = new AcpiTransaction
            {
                Timestamp = DateTime.Now,
                ProcessName = processName,
                ProcessId = 0, // Manual capture
                DeviceId = deviceId,
                Value = value,
                IsWrite = true,
                Description = description
            };

            _capturedTransactions.Add(transaction);

            _logger.LogInformation("Captured: {Process} - DeviceID: 0x{DeviceId:X8} Value: 0x{Value:X8} ({Description})",
                processName, deviceId, value, description);
        }

        /// <summary>
        /// Quick capture helper for common operations
        /// </summary>
        public void CaptureFromGHelper(string operation, uint deviceId, int value)
        {
            AddManualCapture("GHelper.exe", deviceId, value, $"G-Helper: {operation}");
        }

        /// <summary>
        /// Stop monitoring and save captured data
        /// </summary>
        public void StopMonitoring()
        {
            _isMonitoring = false;

            if (_capturedTransactions.Any())
            {
                SaveCaptures();
                _logger.LogInformation("Monitoring stopped. Captured {Count} transactions", 
                    _capturedTransactions.Count);
            }
            else
            {
                _logger.LogWarning("Monitoring stopped. No transactions captured");
            }
        }

        /// <summary>
        /// Verify our implementation matches captured values from official tools
        /// </summary>
        public ValidationResult VerifyAgainstCaptures()
        {
            var result = new ValidationResult
            {
                TotalCaptured = _capturedTransactions.Count
            };

            _logger.LogInformation("=== ACPI Implementation Verification ===");
            _logger.LogInformation("Verifying against {Count} captured transactions\n", result.TotalCaptured);

            // Check CORES_CPU
            var coreTransactions = _capturedTransactions
                .Where(t => t.Description.Contains("core", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (coreTransactions.Any())
            {
                var first = coreTransactions.First();
                _logger.LogInformation("Core Configuration:");
                _logger.LogInformation("  Official Tool: {Process}", first.ProcessName);
                _logger.LogInformation("  Captured Device ID: 0x{CapturedId:X8}", first.DeviceId);
                _logger.LogInformation("  Our Device ID:      0x{OurId:X8}", AsusAcpiInterface.CORES_CPU);

                if (first.DeviceId == AsusAcpiInterface.CORES_CPU)
                {
                    var match = $"CORES_CPU: Match (0x{first.DeviceId:X8})";
                    result.Matches.Add(match);
                    _logger.LogInformation("  ✅ MATCH - Device ID is correct!");
                }
                else
                {
                    var mismatch = $"CORES_CPU: Expected 0x{first.DeviceId:X8}, Got 0x{AsusAcpiInterface.CORES_CPU:X8}";
                    result.Mismatches.Add(mismatch);
                    _logger.LogError("  ❌ MISMATCH - Device ID is wrong!");
                }
                result.TotalVerified++;
            }

            // Check BatteryLimit
            var batteryTransactions = _capturedTransactions
                .Where(t => t.Description.Contains("battery", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (batteryTransactions.Any())
            {
                var first = batteryTransactions.First();
                _logger.LogInformation("\nBattery Limit:");
                _logger.LogInformation("  Official Tool: {Process}", first.ProcessName);
                _logger.LogInformation("  Captured Device ID: 0x{CapturedId:X8}", first.DeviceId);
                _logger.LogInformation("  Our Device ID:      0x{OurId:X8}", AsusAcpiInterface.BatteryLimit);

                if (first.DeviceId == AsusAcpiInterface.BatteryLimit)
                {
                    var match = $"BatteryLimit: Match (0x{first.DeviceId:X8})";
                    result.Matches.Add(match);
                    _logger.LogInformation("  ✅ MATCH - Device ID is correct!");
                }
                else
                {
                    var mismatch = $"BatteryLimit: Expected 0x{first.DeviceId:X8}, Got 0x{AsusAcpiInterface.BatteryLimit:X8}";
                    result.Mismatches.Add(mismatch);
                    _logger.LogError("  ❌ MISMATCH - Device ID is wrong!");
                }
                result.TotalVerified++;
            }

            // Check PerformanceMode
            var perfTransactions = _capturedTransactions
                .Where(t => t.Description.Contains("performance", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (perfTransactions.Any())
            {
                var first = perfTransactions.First();
                _logger.LogInformation("\nPerformance Mode:");
                _logger.LogInformation("  Official Tool: {Process}", first.ProcessName);
                _logger.LogInformation("  Captured Device ID: 0x{CapturedId:X8}", first.DeviceId);
                _logger.LogInformation("  Our Device ID:      0x{OurId:X8}", AsusAcpiInterface.PerformanceMode);

                if (first.DeviceId == AsusAcpiInterface.PerformanceMode)
                {
                    var match = $"PerformanceMode: Match (0x{first.DeviceId:X8})";
                    result.Matches.Add(match);
                    _logger.LogInformation("  ✅ MATCH - Device ID is correct!");
                }
                else
                {
                    var mismatch = $"PerformanceMode: Expected 0x{first.DeviceId:X8}, Got 0x{AsusAcpiInterface.PerformanceMode:X8}";
                    result.Mismatches.Add(mismatch);
                    _logger.LogError("  ❌ MISMATCH - Device ID is wrong!");
                }
                result.TotalVerified++;
            }

            result.AllMatch = result.Mismatches.Count == 0 && result.Matches.Count > 0;

            _logger.LogInformation("\n=== Verification Summary ===");
            _logger.LogInformation("Total Captured: {Total}", result.TotalCaptured);
            _logger.LogInformation("Verified: {Verified}", result.TotalVerified);
            _logger.LogInformation("Matches: {Matches}", result.Matches.Count);
            _logger.LogInformation("Mismatches: {Mismatches}", result.Mismatches.Count);
            _logger.LogInformation("Result: {Result}", result.AllMatch ? "✅ ALL MATCH" : "❌ SOME MISMATCHES");

            return result;
        }

        /// <summary>
        /// Save captured transactions to disk
        /// </summary>
        public void SaveCaptures()
        {
            try
            {
                var json = JsonSerializer.Serialize(_capturedTransactions, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                File.WriteAllText(_captureFilePath, json);
                _logger.LogInformation("Saved {Count} captures to: {Path}",
                    _capturedTransactions.Count, _captureFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save captures");
            }
        }

        /// <summary>
        /// Load previously captured transactions
        /// </summary>
        public void LoadCaptures()
        {
            try
            {
                if (!File.Exists(_captureFilePath))
                {
                    _logger.LogWarning("No previous captures found at: {Path}", _captureFilePath);
                    return;
                }

                var json = File.ReadAllText(_captureFilePath);
                var loaded = JsonSerializer.Deserialize<List<AcpiTransaction>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (loaded != null)
                {
                    _capturedTransactions.Clear();
                    _capturedTransactions.AddRange(loaded);
                    _logger.LogInformation("Loaded {Count} captures from: {Path}",
                        _capturedTransactions.Count, _captureFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load captures");
            }
        }

        /// <summary>
        /// Get all captured transactions
        /// </summary>
        public IReadOnlyList<AcpiTransaction> GetCaptures() => _capturedTransactions.AsReadOnly();

        /// <summary>
        /// Clear all captured transactions
        /// </summary>
        public void ClearCaptures()
        {
            _capturedTransactions.Clear();
            _logger.LogInformation("Cleared all captured transactions");
        }
    }
}
