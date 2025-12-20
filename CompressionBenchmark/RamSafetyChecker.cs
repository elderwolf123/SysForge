using System;

namespace CompressionBenchmark;

/// <summary>
/// Checks system RAM and enforces safety limits for compression testing.
/// Prevents crashes from testing files larger than available RAM.
/// </summary>
public class RamSafetyChecker
{
    public long GetAvailableRamBytes()
    {
        try
        {
            // Use GC to estimate available memory
            var gcInfo = GC.GetGCMemoryInfo();
            var totalMemory = gcInfo.TotalAvailableMemoryBytes;
            var usedMemory = GC.GetTotalMemory(false);
            return totalMemory - usedMemory;
        }
        catch
        {
            // Fallback: assume 8GB available if we can't check
            return 8L * 1024 * 1024 * 1024;
        }
    }

    public long GetTotalRamBytes()
    {
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            return gcInfo.TotalAvailableMemoryBytes;
        }
        catch
        {
            return 32L * 1024 * 1024 * 1024; // Assume 32GB
        }
    }

    public bool CanTestFile(long fileSize, out string reason)
    {
        var availableRam = GetAvailableRamBytes();
        
        // HARDCODED: Keep exactly 5GB reserved for system stability
        // Everything beyond this can be used for compression
        const long RESERVED_BUFFER = 5L * 1024 * 1024 * 1024; // 5GB fixed
        var usableRam = availableRam - RESERVED_BUFFER;
        
        if (usableRam <= 0)
        {
            reason = $"Not enough RAM available. Need at least {FormatBytes(RESERVED_BUFFER)} + file size";
            return false;
        }
        
        if (fileSize > usableRam)
        {
            reason = $"File size ({FormatBytes(fileSize)}) exceeds usable RAM " +
                    $"({FormatBytes(usableRam)}) with {FormatBytes(RESERVED_BUFFER)} reserved";
            return false;
        }

        // Hard limit: Never test files >10GB regardless of RAM
        if (fileSize > 10L * 1024 * 1024 * 1024)
        {
            reason = $"File size ({FormatBytes(fileSize)}) exceeds hard limit of 10GB";
            return false;
        }

        reason = "";
        return true;
    }

    public void PrintRamStatus()
    {
        var available = GetAvailableRamBytes();
        const long RESERVED_BUFFER = 5L * 1024 * 1024 * 1024; // 5GB fixed
        var usable = Math.Max(0, available - RESERVED_BUFFER);

        Console.WriteLine("\n💾 RAM STATUS:");
        Console.WriteLine($"   Total available: {FormatBytes(available)}");
        Console.WriteLine($"   Reserved (fixed): {FormatBytes(RESERVED_BUFFER)}");
        Console.WriteLine($"   Usable for testing: {FormatBytes(usable)}");
        Console.WriteLine($"   Hard file limit: 10 GB");
        Console.WriteLine($"\n   💡 Compression will use ALL {FormatBytes(usable)} available\n");
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
