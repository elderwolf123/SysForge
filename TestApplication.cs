using RamOptimizer.HardwareControl;

Console.WriteLine("=== ASUS ROG Flow Z13 Hardware Control Test ===\n");

// Check if ASUS WMI is available
Console.WriteLine("1. Checking ASUS WMI Interface...");
if (!AsusWmiInterface.IsAvailable())
{
    Console.WriteLine("   ❌ ASUS WMI Interface NOT FOUND!");
    Console.WriteLine("   Make sure ASUS System Control Interface driver is installed.");
    Console.WriteLine("   Press any key to exit...");
    Console.ReadKey();
    return;
}
Console.WriteLine("   ✅ ASUS WMI Interface detected!\n");

try
{
    using var wmi = new AsusWmiInterface();
    var perfManager = new PerformanceModeManager(wmi);
    var gpuController = new GpuModeController(wmi);
    var batteryManager = new BatteryManager(wmi);
    var monitor = new HardwareMonitor();

    // Test Performance Mode
    Console.WriteLine("2. Testing Performance Modes...");
    try
    {
        var currentMode = perfManager.GetCurrentMode();
        Console.WriteLine($"   Current Mode: {PerformanceModeManager.GetModeName(currentMode)}");
        Console.WriteLine($"   Description: {PerformanceModeManager.GetModeDescription(currentMode)}");
        Console.WriteLine("   ✅ Performance mode reading works!\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ❌ Error: {ex.Message}\n");
    }

    // Test GPU Mode
    Console.WriteLine("3. Testing GPU Modes...");
    try
    {
        var currentGpuMode = gpuController.GetCurrentMode();
        Console.WriteLine($"   Current GPU Mode: {GpuModeController.GetModeName(currentGpuMode)}");
        Console.WriteLine($"   Description: {GpuModeController.GetModeDescription(currentGpuMode)}");
        Console.WriteLine("   ✅ GPU mode reading works!\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ❌ Error: {ex.Message}\n");
    }

    // Test Battery Charge Limit
    Console.WriteLine("4. Testing Battery Charge Limit...");
    try
    {
        var chargeLimit = batteryManager.GetChargeLimit();
        Console.WriteLine($"   Current Charge Limit: {chargeLimit}%");
        Console.WriteLine($"   Description: {BatteryManager.GetLimitDescription(chargeLimit)}");
        Console.WriteLine("   ✅ Battery charge limit reading works!\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ❌ Error: {ex.Message}\n");
    }

    // Test Hardware Monitoring
    Console.WriteLine("5. Testing Hardware Monitoring...");
    try
    {
        var cpuTemp = monitor.GetCpuTemperature();
        var cpuUsage = monitor.GetCpuUsage();
        var powerInfo = monitor.GetPowerInfo();

        Console.WriteLine($"   CPU Temperature: {cpuTemp:F1}°C");
        Console.WriteLine($"   CPU Usage: {cpuUsage:F1}%");
        Console.WriteLine($"   Power Status: {(powerInfo.IsPluggedIn ? "Plugged In" : "On Battery")}");
        Console.WriteLine($"   Battery: {powerInfo.BatteryPercentage}%");
        Console.WriteLine("   ✅ Hardware monitoring works!\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ❌ Error: {ex.Message}\n");
    }

    // Interactive test menu
    Console.WriteLine("\n=== Interactive Tests ===");
    Console.WriteLine("Would you like to test changing settings? (y/n)");
    var response = Console.ReadLine()?.ToLower();

    if (response == "y")
    {
        while (true)
        {
            Console.WriteLine("\nSelect a test:");
            Console.WriteLine("1. Change Performance Mode");
            Console.WriteLine("2. Change GPU Mode (requires restart)");
            Console.WriteLine("3. Change Battery Charge Limit");
            Console.WriteLine("4. Exit");
            Console.Write("Choice: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    TestPerformanceModeChange(perfManager);
                    break;
                case "2":
                    TestGpuModeChange(gpuController);
                    break;
                case "3":
                    TestBatteryLimitChange(batteryManager);
                    break;
                case "4":
                    return;
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Fatal Error: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

static void TestPerformanceModeChange(PerformanceModeManager manager)
{
    Console.WriteLine("\nSelect Performance Mode:");
    Console.WriteLine("0. Silent");
    Console.WriteLine("1. Balanced");
    Console.WriteLine("2. Turbo");
    Console.Write("Choice: ");

    if (int.TryParse(Console.ReadLine(), out int mode) && mode >= 0 && mode <= 2)
    {
        try
        {
            manager.SetMode((PerformanceMode)mode);
            Console.WriteLine($"✅ Performance mode set to {PerformanceModeManager.GetModeName((PerformanceMode)mode)}");
            
            // Verify
            var newMode = manager.GetCurrentMode();
            Console.WriteLine($"   Verified: {PerformanceModeManager.GetModeName(newMode)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }
}

static void TestGpuModeChange(GpuModeController controller)
{
    Console.WriteLine("\nSelect GPU Mode:");
    Console.WriteLine("0. Eco (iGPU only)");
    Console.WriteLine("1. Standard (Hybrid)");
    Console.WriteLine("2. Ultimate (dGPU drives display)");
    Console.Write("Choice: ");

    if (int.TryParse(Console.ReadLine(), out int mode) && mode >= 0 && mode <= 2)
    {
        try
        {
            controller.SetMode((GpuMode)mode);
            Console.WriteLine($"✅ GPU mode set to {GpuModeController.GetModeName((GpuMode)mode)}");
            Console.WriteLine("   ⚠️ RESTART REQUIRED for GPU mode change to take effect!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }
}

static void TestBatteryLimitChange(BatteryManager manager)
{
    Console.WriteLine("\nEnter battery charge limit (60-100): ");
    if (int.TryParse(Console.ReadLine(), out int limit) && limit >= 60 && limit <= 100)
    {
        try
        {
            manager.SetChargeLimit(limit);
            Console.WriteLine($"✅ Battery charge limit set to {limit}%");
            Console.WriteLine($"   {BatteryManager.GetLimitDescription(limit)}");
            
            // Verify
            var newLimit = manager.GetChargeLimit();
            Console.WriteLine($"   Verified: {newLimit}%");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }
}