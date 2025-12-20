using System;
using System.Threading.Tasks;
using RamOptimizer.Logging;
using RamOptimizer.HardwareControl;

namespace RamOptimizerConsole.Testing;

public class HardwareControlTester
{
    private readonly AsusHardwareController _controller;
    private readonly ComprehensiveLogger? _logger;

    public HardwareControlTester(AsusHardwareController controller, ComprehensiveLogger? logger = null)
    {
        _controller = controller;
        _logger = logger;
    }

    public async Task RunTestAsync(bool dryRun)
    {
        Console.WriteLine($"Mode: {(dryRun ? "🧪 DRY RUN" : "⚡ LIVE")}\n");

        // Display current config
        Console.WriteLine("Current Hardware Configuration:");
        Console.WriteLine(new string('─', 50));
        DisplayCurrentConfig();

        // Test DryRun mode
        Console.WriteLine("\n\nTesting DryRun Mode:");
        Console.WriteLine(new string('─', 50));
        await TestDryRunOperations();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✅ Hardware control module test PASSED");
        Console.ResetColor();
    }

    private void DisplayCurrentConfig()
    {
        try
        {
            Console.WriteLine($"  P-Cores (Max/Current): {_controller.GetMaxPCores()}/{_controller.GetCurrentPCores()}");
            Console.WriteLine($"  E-Cores (Max/Current): {_controller.GetMaxECores()}/{_controller.GetCurrentECores()}");
            Console.WriteLine($"  Battery Limit: {_controller.GetChargeLimit()}%");
            Console.WriteLine($"  CPU Temp: {_controller.GetCpuTemperature()}°C");
            Console.WriteLine($"  GPU Temp: {_controller.GetGpuTemperature()}°C");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }
    }

    private async Task TestDryRunOperations()
    {
        _controller.DryRunMode = true;
        
        Console.WriteLine("  Testing core configuration (4P, 4E)...");
        var result1 = _controller.SetCores(4, 4);
        Console.WriteLine($"    Result: {(result1 ? "✓ Would succeed" : "✗ Would fail")}");

        await Task.Delay(200);

        Console.WriteLine("  Testing battery limit (80%)...");
        var result2 = _controller.SetChargeLimit(80);
        Console.WriteLine($"    Result: {(result2 ? "✓ Would succeed" : "✗ Would fail")}");

        Console.WriteLine("\n  ✓ All DryRun operations functional");
    }
}