using System;
using Microsoft.Extensions.Logging;
using RamOptimizer.Core.Plugins;
using RamOptimizer.HardwareControl;
using RamOptimizer.Plugins.Asus;
using RamOptimizer.Plugins.Generic;

namespace RamOptimizer.Examples;

/// <summary>
/// Complete example of using the modular plugin architecture
/// </summary>
public class PluginUsageExample
{
    public static void Main(string[] args)
    {
        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<PluginUsageExample>();

        logger.LogInformation("=== ACPI Safety System - Plugin Architecture Demo ===\n");

        // Step 1: Create plugin registry
        var registry = new PluginRegistry(loggerFactory.CreateLogger<PluginRegistry>());

        // Step 2: Register all available plugins
        logger.LogInformation("Registering plugins...");
        registry.RegisterPlugin(new AsusRogPlugin());
        registry.RegisterPlugin(new GenericWindowsPlugin());
        logger.LogInformation($"Registered {registry.GetAllPlugins().Count} plugins\n");

        // Step 3: Auto-detect best plugin for current system
        logger.LogInformation("Auto-detecting hardware...");
        var bestPlugin = registry.FindBestPlugin();

        if (bestPlugin == null)
        {
            logger.LogError("No compatible plugin found for this system!");
            return;
        }

        logger.LogInformation($"✅ Best plugin: {bestPlugin.PluginName} (v{bestPlugin.PluginVersion})");
        logger.LogInformation($"   Plugin ID: {bestPlugin.PluginId}");
        logger.LogInformation($"   Capabilities: {bestPlugin.GetCapabilities()}\n");

        // Step 4: Create hardware controller
        logger.LogInformation("Creating hardware controller...");
        using var rawController = bestPlugin.CreateController();
        
        if (!rawController.Initialize())
        {
            logger.LogError("Failed to initialize hardware controller!");
            return;
        }

        logger.LogInformation($"✅ Controller initialized");
        logger.LogInformation($"   Device: {rawController.GetDeviceIdentifier()}");
        logger.LogInformation($"   Type: {rawController.GetDeviceType()}\n");

        // Step 5: Wrap with safety layer
        logger.LogInformation("Creating safe hardware controller with protection layers...");
        using var safeController = new SafeHardwareController(
            rawController, 
            loggerFactory.CreateLogger<SafeHardwareController>()
        );

        logger.LogInformation("✅ Safe controller created with:");
        logger.LogInformation("   - Pre-flight validation");
        logger.LogInformation("   - Automatic snapshotting");
        logger.LogInformation("   - Rollback protection");
        logger.LogInformation("   - Read-after-write verification\n");

        // Step 6: Demonstrate capabilities
        DemonstrateCapabilities(safeController, logger);

        // Step 7: Show test mode
        logger.LogInformation("\n=== Test Mode Demo ===");
        safeController.TestModeEnabled = true;
        logger.LogInformation("Test mode ENABLED - all operations will be simulated\n");

        if (safeController is RamOptimizer.Core.Interfaces.ICoreController coreCtrl && coreCtrl.IsSupported)
        {
            logger.LogInformation("Attempting to set cores to 4P + 6E (SIMULATED)...");
            bool result = safeController.SetCores(4, 6);
            logger.LogInformation($"Result: {(result ? "Success (simulated)" : "Failed")}\n");
        }

        logger.LogInformation("=== Demo Complete ===");
    }

    private static void DemonstrateCapabilities(SafeHardwareController controller, ILogger logger)
    {
        logger.LogInformation("=== Hardware Capabilities ===\n");

        // Check core control
        if (controller is RamOptimizer.Core.Interfaces.ICoreController coreCtrl && coreCtrl.IsSupported)
        {
            logger.LogInformation("✅ Core Control: SUPPORTED");
            logger.LogInformation($"   Max P-Cores: {coreCtrl.GetMaxPCores()}");
            logger.LogInformation($"   Max E-Cores: {coreCtrl.GetMaxECores()}");
            logger.LogInformation($"   Current P-Cores: {coreCtrl.GetCurrentPCores()}");
            logger.LogInformation($"   Current E-Cores: {coreCtrl.GetCurrentECores()}");
        }
        else
        {
            logger.LogWarning("❌ Core Control: NOT SUPPORTED");
        }

        // Check battery control
        if (controller is RamOptimizer.Core.Interfaces.IBatteryController batCtrl && batCtrl.IsSupported)
        {
            logger.LogInformation("\n✅ Battery Control: SUPPORTED");
            logger.LogInformation($"   Current Limit: {batCtrl.GetChargeLimit()}%");
            logger.LogInformation($"   Range: {batCtrl.GetMinLimit()}% - {batCtrl.GetMaxLimit()}%");
        }
        else
        {
            logger.LogWarning("\n❌ Battery Control: NOT SUPPORTED");
        }

        // Check performance control
        if (controller is RamOptimizer.Core.Interfaces.IPerformanceController perfCtrl && perfCtrl.IsSupported)
        {
            logger.LogInformation("\n✅ Performance Control: SUPPORTED");
            logger.LogInformation($"   Current Mode: {perfCtrl.GetCurrentMode()}");
            logger.LogInformation($"   Available Modes: {string.Join(", ", perfCtrl.GetAvailableModes())}");
        }
        else
        {
            logger.LogWarning("\n❌ Performance Control: NOT SUPPORTED");
        }

        // Check rollback status
        logger.LogInformation($"\n🔒 Rollback Protection: {(controller.IsRollbackPending() ? "PENDING" : "Ready")}");
        
        // Show snapshot info
        var snapshotMgr = controller.GetSnapshotManager();
        logger.LogInformation($"📸 Snapshots: {snapshotMgr.GetSnapshotCount()} saved");
    }
}
