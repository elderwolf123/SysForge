using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompressionBenchmark;

/// <summary>
/// Scans all drives to discover file types for compression testing.
/// Builds a comprehensive list of real-world file types on the user's system.
/// </summary>
public class DriveScanner
{
    private readonly FileTypeDatabase _database;
    private readonly HashSet<string> _discoveredExtensions = new();
    private long _filesScanned = 0;
    private long _directoriesScanned = 0;

    public DriveScanner(FileTypeDatabase database)
    {
        _database = database;
    }

    public void ScanAllDrives()
    {
        Console.WriteLine("🔍 Starting drive scan...\n");

        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType != DriveType.Network)
            .ToList();

        Console.WriteLine($"Found {drives.Count} accessible drives:");
        foreach (var drive in drives)
        {
            Console.WriteLine($"  {drive.Name} ({drive.DriveType}) - {FormatBytes(drive.AvailableFreeSpace)} free");
        }

        Console.WriteLine();

        foreach (var drive in drives)
        {
            try
            {
                Console.WriteLine($"\n📂 Scanning {drive.Name}...");
                ScanDrive(drive);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error scanning {drive.Name}: {ex.Message}");
            }
        }

        PrintScanSummary();
    }

    private void ScanDrive(DriveInfo drive)
    {
        try
        {
            ScanDirectory(drive.RootDirectory);
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"⚠️  Access denied to {drive.Name}");
        }
    }

    private void ScanDirectory(DirectoryInfo dir)
    {
        _directoriesScanned++;

        if (_directoriesScanned % 1000 == 0)
        {
            Console.Write($"\r  Scanned: {_directoriesScanned:N0} dirs, {_filesScanned:N0} files, {_discoveredExtensions.Count} file types");
        }

        try
        {
            // Scan files in current directory
            foreach (var file in dir.GetFiles())
            {
                _filesScanned++;
                
                var extension = file.Extension.ToLowerInvariant();
                if (string.IsNullOrEmpty(extension))
                    extension = ".no-extension";

                // Add to database with file size for diversity
                _database.AddDiscoveredFileType(extension, file.FullName, file.Length);
                _discoveredExtensions.Add(extension);
            }

            // Recursively scan subdirectories (with protection)
            foreach (var subDir in dir.GetDirectories())
            {
                // Skip system/hidden directories
                if (ShouldSkipDirectory(subDir))
                    continue;

                try
                {
                    ScanDirectory(subDir);
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip inaccessible directories silently
                }
                catch (Exception)
                {
                    // Skip problematic directories
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip inaccessible directories
        }
    }

    private bool ShouldSkipDirectory(DirectoryInfo dir)
    {
        // Skip system directories
        var skipDirs = new[]
        {
            "$Recycle.Bin", "System Volume Information", "Recovery",
            "ProgramData", "Windows", "hiberfil.sys", "pagefile.sys",
            ".git", "node_modules", "__pycache__", ".vs", "obj", "bin"
        };

        return skipDirs.Any(skip => dir.Name.Equals(skip, StringComparison.OrdinalIgnoreCase))
            || dir.Attributes.HasFlag(FileAttributes.System)
            || dir.Attributes.HasFlag(FileAttributes.Hidden);
    }

    private void PrintScanSummary()
    {
        Console.WriteLine("\n\n" + new string('=', 60));
        Console.WriteLine("DRIVE SCAN COMPLETE");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"📁 Directories scanned: {_directoriesScanned:N0}");
        Console.WriteLine($"📄 Files scanned:       {_filesScanned:N0}");
        Console.WriteLine($"🎯 File types found:    {_discoveredExtensions.Count}");
        Console.WriteLine(new string('=', 60) + "\n");
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
