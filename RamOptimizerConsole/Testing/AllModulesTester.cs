using System;
using System.Threading.Tasks;
using RamOptimizer.Logging;
using RamOptimizer.HardwareControl;

namespace RamOptimizerConsole.Testing;

public class AllModulesTester
{
    private readonly AsusHardwareController? _hardwareController;
    private readonly ComprehensiveLogger? _logger;

    public AllModulesTester(AsusHardwareController? hardwareController, ComprehensiveLogger? logger)
    {
        _hardwareController = hardwareController;
        _logger = logger;
    }

    public async Task RunAllTestsAsync(bool dryRun)
    {
        Console.WriteLine("🧪 COMPREHENSIVE MODULE TESTING\n");
        Console.WriteLine(new string('═', 70) + "\n");

        int passed = 0;
        int total = 0;

        // Test RAM Optimization
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("▶ Testing RAM Optimization Module...");
        Console.ResetColor();
        total++;
        try
        {
            var tester = new RAMOptimizationTester(_logger);
            await tester.RunTestAsync(dryRun);
            passed++;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✅ PASSED\n");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ❌ FAILED: {ex.Message}\n");
            Console.ResetColor();
        }

        // Test Hardware Control
        if (_hardwareController != null)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("▶ Testing Hardware Control Module...");
            Console.ResetColor();
            total++;
            try
            {
                var tester = new HardwareControlTester(_hardwareController, _logger);
                await tester.RunTestAsync(dryRun);
                passed++;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✅ PASSED\n");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ❌ FAILED: {ex.Message}\n");
                Console.ResetColor();
            }
        }

        // Test Compression
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("▶ Testing File Compression Module...");
        Console.ResetColor();
        total++;
        try
        {
            var tester = new CompressionTester(_logger);
            await tester.RunTestAsync(dryRun);
            passed++;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✅ PASSED\n");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ❌ FAILED: {ex.Message}\n");
            Console.ResetColor();
        }

        // Summary
        Console.WriteLine(new string('═', 70));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"TEST SUMMARY: {passed}/{total} modules passed");
        Console.ResetColor();
        Console.WriteLine(new string('═', 70));

        if (passed == total)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n🎉 All modules are operational!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n⚠️  {total - passed} module(s) had issues");
            Console.ResetColor();
        }
    }
}