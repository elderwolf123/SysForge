using System;
using System.Diagnostics;
using RamOptimizer.HardwareControl;

Console.WriteLine("=== Hardware Monitoring Diagnostic ===\n");

// Test 1: Performance Counters
Console.WriteLine("Test 1: Performance Counters");
Console.WriteLine("-----------------------------");

try
{
    var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
    cpuCounter.NextValue(); // First call always returns 0
    System.Threading.Thread.Sleep(1000);
    var cpuValue = cpuCounter.NextValue();
    Console.WriteLine($"✓ CPU Counter: {cpuValue:F1}%");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ CPU Counter Failed: {ex.Message}");
}

try
{
    var memCounter = new PerformanceCounter("Memory", "Available MBytes");
    var memValue = memCounter.NextValue();
    Console.WriteLine($"✓ Memory Counter: {memValue:F0} MB available");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Memory Counter Failed: {ex.Message}");
}

try
{
    var diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
    diskCounter.NextValue();
    System.Threading.Thread.Sleep(1000);
    var diskValue = diskCounter.NextValue();
    Console.WriteLine($"✓ Disk Counter: {diskValue:F1}%");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Disk Counter Failed: {ex.Message}");
}

// Test 2: Thermal Zone
Console.WriteLine("\nTest 2: Thermal Zone Counter");
Console.WriteLine("-----------------------------");

try
{
    // List all thermal zone instances
    var category = new PerformanceCounterCategory("Thermal Zone Information");
    var instances = category.GetInstanceNames();
    
    Console.WriteLine($"Found {instances.Length} thermal zones:");
    foreach (var instance in instances)
    {
        Console.WriteLine($"  - {instance}");
        try
        {
            var tempCounter = new PerformanceCounter("Thermal Zone Information", "Temperature", instance);
            var kelvin = tempCounter.NextValue();
            var celsius = (kelvin / 10.0f) - 273.15f;
            Console.WriteLine($"    Temperature: {celsius:F1}°C (raw: {kelvin})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    Error reading: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Thermal Zone Failed: {ex.Message}");
}

// Test 3: ACPI Interface
Console.WriteLine("\nTest 3: ASUS ACPI Interface");
Console.WriteLine("-----------------------------");

try
{
    if (AsusAcpiInterface.IsAvailable())
    {
        Console.WriteLine("✓ ACPI Interface Available");
        
        using var acpi = new AsusAcpiInterface();
        
        // Test CPU temp via ACPI
        var cpuTempRaw = acpi.DeviceGet(AsusAcpiInterface.Temp_CPU);
        Console.WriteLine($"  CPU Temp (raw): 0x{cpuTempRaw:X8} ({cpuTempRaw})");
        
        // Test CPU fan
        var cpuFanRaw = acpi.DeviceGet(AsusAcpiInterface.CPU_Fan);
        Console.WriteLine($"  CPU Fan (raw): 0x{cpuFanRaw:X8} ({cpuFanRaw})");
        
        // Test battery limit
        var batteryRaw = acpi.DeviceGet(AsusAcpiInterface.BatteryLimit);
        var batteryLimit = ((batteryRaw >> 16) & 0xFF) - 36;
        Console.WriteLine($"  Battery Limit (raw): 0x{batteryRaw:X8}");
        Console.WriteLine($"  Battery Limit: {batteryLimit}%");
        
        // Test performance mode
        var perfModeRaw = acpi.DeviceGet(AsusAcpiInterface.PerformanceMode);
        Console.WriteLine($"  Performance Mode (raw): 0x{perfModeRaw:X8} ({perfModeRaw})");
    }
    else
    {
        Console.WriteLine("✗ ACPI Interface Not Available");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ ACPI Test Failed: {ex.Message}");
    Console.WriteLine($"   Stack: {ex.StackTrace}");
}

Console.WriteLine("\n=== Diagnostic Complete ===");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
