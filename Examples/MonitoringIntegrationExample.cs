using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RamOptimizer.HardwareControl;
using RamOptimizer.HardwareControl.Monitoring;

namespace RamOptimizer.Examples
{
    /// <summary>
    /// Example showing how to use ACPI monitoring to validate device IDs
    /// </summary>
    public class MonitoringIntegrationExample
    {
        private readonly ILogger<MonitoringIntegrationExample> _logger;
        private readonly AcpiMonitoringService _monitoring;

        public MonitoringIntegrationExample(
            ILogger<MonitoringIntegrationExample> logger,
            AcpiMonitoringService monitoring)
        {
            _logger = logger;
            _monitoring = monitoring;
        }

        /// <summary>
        /// Method 1: Manual capture from Procmon observations
        /// </summary>
        public async Task ManualCaptureWorkflow()
        {
            _logger.LogInformation("=== Manual ACPI Capture Workflow ===");
            _logger.LogInformation("1. Download and run Process Monitor (Procmon)");
            _logger.LogInformation("2. Set filter: Process Name is GHelper.exe");
            _logger.LogInformation("3. Set filter: Operation is DeviceIoControl");
            _logger.LogInformation("4. Open G-Helper and make changes");
            _logger.LogInformation("5. Observe ACPI calls in Procmon");
            _logger.LogInformation("6. Manually enter them here\n");

            // Example: User observes these calls in Procmon and enters them
            _logger.LogInformation("Example captures from Procmon:");

            // Capture: G-Helper sets cores to 6P + 8E
            _monitoring.CaptureFromGHelper(
                operation: "Set cores to 6P + 8E",
                deviceId: 0x001200D2,  // From Procmon
                value: 0x0608           // From Procmon
            );

            // Capture: G-Helper sets battery limit to 80%
            _monitoring.CaptureFromGHelper(
                operation: "Set battery limit to 80%",
                deviceId: 0x00120057,  // From Procmon
                value: 116              // From Procmon (80 + 36 offset)
            );

            // Capture: G-Helper sets performance mode to Turbo
            _monitoring.CaptureFromGHelper(
                operation: "Set performance mode to Turbo",
                deviceId: 0x00120075,  // From Procmon
                value: 2                // From Procmon
            );

            // Save captures
            _monitoring.SaveCaptures();

            _logger.LogInformation("\nCaptures saved. Ready for verification.");
        }

        /// <summary>
        /// Method 2: Load previous captures and verify
        /// </summary>
        public void VerifyImplementation()
        {
            _logger.LogInformation("=== Verifying Implementation ===\n");

            // Load previously saved captures
            _monitoring.LoadCaptures();

            // Verify our device IDs match
            var result = _monitoring.VerifyAgainstCaptures();

            if (result.AllMatch)
            {
                _logger.LogInformation("\n✅ SUCCESS: Our implementation matches G-Helper!");
                _logger.LogInformation("Safe to proceed with ACPI operations.");
            }
            else
            {
                _logger.LogError("\n❌ FAILURE: Device ID mismatches detected!");
                _logger.LogError("DO NOT PROCEED with ACPI writes until resolved.");
                
                if (result.Mismatches.Any())
                {
                    _logger.LogError("\nMismatches:");
                    foreach (var mismatch in result.Mismatches)
                    {
                        _logger.LogError("  - {Mismatch}", mismatch);
                    }
                }
            }
        }

        /// <summary>
        /// Method 3: Automated workflow - capture, verify, and proceed if safe
        /// </summary>
        public async Task<bool> AutomatedValidationWorkflow()
        {
            _logger.LogInformation("=== Automated ACPI Validation Workflow ===\n");

            // Step 1: Check if we have previous captures
            _monitoring.LoadCaptures();
            
            if (_monitoring.GetCaptures().Count == 0)
            {
                _logger.LogWarning("No previous captures found.");
                _logger.LogWarning("Please run manual capture workflow first:");
                _logger.LogWarning("1. Use Procmon to observe G-Helper");
                _logger.LogWarning("2. Call ManualCaptureWorkflow()");
                _logger.LogWarning("3. Then retry this method");
                return false;
            }

            // Step 2: Verify implementation
            var result = _monitoring.VerifyAgainstCaptures();

            // Step 3: Return verification result
            return result.AllMatch;
        }

        /// <summary>
        /// Integration with SafeHardwareController - only allow writes if validation passed
        /// </summary>
        public async Task<SafeHardwareController> CreateValidatedSafeInterface(ILogger logger)
        {
            _logger.LogInformation("=== Creating Validated ACPI Interface ===\n");

            // Step 1: Verify device IDs
            bool validationPassed = await AutomatedValidationWorkflow();

            if (!validationPassed)
            {
                _logger.LogError("Validation FAILED - ACPI interface will be created in READ-ONLY mode");
                _logger.LogError("Fix device ID mismatches before proceeding");
            }

            // Step 2: Create SafeHardwareController via Plugin
            var plugin = new RamOptimizer.Plugins.Asus.AsusRogPlugin();
            if (!plugin.CanHandle())
            {
                _logger.LogError("ASUS ROG Plugin cannot handle this system");
                // Fallback to generic or throw
                throw new PlatformNotSupportedException("This system is not supported by the ASUS ROG Plugin");
            }

            var rawController = plugin.CreateController();
            var safeController = new SafeHardwareController(rawController, logger);

            // Step 3: Configure based on validation result
            if (!validationPassed)
            {
                // Force test mode if validation failed
                safeController.TestModeEnabled = true;
                _logger.LogWarning("FORCED TEST MODE - Validation did not pass");
                _logger.LogWarning("All operations will be simulated (no hardware writes)");
            }
            else
            {
                _logger.LogInformation("✅ Validation passed - Real mode available");
                _logger.LogInformation("Use TestModeEnabled property to control mode");
            }

            return safeController;
        }
    }

    /// <summary>
    /// Example console app showing complete workflow
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Setup logging
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            var logger = loggerFactory.CreateLogger<Program>();
            var monitoringLogger = loggerFactory.CreateLogger<AcpiMonitoringService>();
            var exampleLogger = loggerFactory.CreateLogger<MonitoringIntegrationExample>();

            // Create services
            var monitoring = new AcpiMonitoringService(monitoringLogger);
            var example = new MonitoringIntegrationExample(exampleLogger, monitoring);

            logger.LogInformation("=== ACPI Monitoring & Validation Example ===\n");

            // Interactive menu
            while (true)
            {
                Console.WriteLine("\nChoose an option:");
                Console.WriteLine("1. Manual Capture Workflow (from Procmon observations)");
                Console.WriteLine("2. Verify Implementation");
                Console.WriteLine("3. Automated Validation");
                Console.WriteLine("4. Create Validated Safe Interface");
                Console.WriteLine("5. View Captured Transactions");
                Console.WriteLine("6. Clear Captures");
                Console.WriteLine("0. Exit");
                Console.Write("\nChoice: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await example.ManualCaptureWorkflow();
                        break;

                    case "2":
                        example.VerifyImplementation();
                        break;

                    case "3":
                        var passed = await example.AutomatedValidationWorkflow();
                        if (passed)
                        {
                            Console.WriteLine("\n✅ Safe to proceed with ACPI operations");
                        }
                        else
                        {
                            Console.WriteLine("\n❌ NOT safe - validation failed");
                        }
                        break;

                    case "4":
                        var safeLogger = loggerFactory.CreateLogger<SafeHardwareController>();
                        try 
                        {
                            var safeController = await example.CreateValidatedSafeInterface(safeLogger);
                            Console.WriteLine($"\nCreated SafeHardwareController (TestMode: {safeController.TestModeEnabled})");
                            safeController.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"\nFailed to create controller: {ex.Message}");
                        }
                        break;

                    case "5":
                        monitoring.LoadCaptures();
                        var captures = monitoring.GetCaptures();
                        Console.WriteLine($"\n=== Captured Transactions ({captures.Count}) ===");
                        foreach (var capture in captures)
                        {
                            Console.WriteLine($"[{capture.Timestamp:yyyy-MM-dd HH:mm:ss}] {capture.ProcessName}");
                            Console.WriteLine($"  DeviceID: 0x{capture.DeviceId:X8} Value: 0x{capture.Value:X8}");
                            Console.WriteLine($"  {capture.Description}");
                            Console.WriteLine();
                        }
                        break;

                    case "6":
                        monitoring.ClearCaptures();
                        Console.WriteLine("\n✅ All captures cleared");
                        break;

                    case "0":
                        return;

                    default:
                        Console.WriteLine("\nInvalid choice");
                        break;
                }
            }
        }
    }
}
