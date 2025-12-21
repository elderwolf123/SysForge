using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace RamOptimizerNova.Services;

public class InstalledProgram
{
    public string Name { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool IsCompressed { get; set; }
    public string Type { get; set; } = "Application"; // "Game" or "Application"
    public string SizeFormatted => FormatBytes(SizeBytes);

    private static string FormatBytes(long bytes)
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

public class ProgramScanner
{
    private readonly WindowsCompactCompression _compressor;

    public ProgramScanner()
    {
        _compressor = new WindowsCompactCompression();
    }

    public async Task<List<InstalledProgram>> ScanInstalledProgramsAsync()
    {
        var programs = new List<InstalledProgram>();

        // Scan common game/program locations
        await Task.Run(async () =>
        {
            // 1. Scan Program Files
            programs.AddRange(await ScanDirectory(@"C:\Program Files", "Application"));
            programs.AddRange(await ScanDirectory(@"C:\Program Files (x86)", "Application"));

            // 2. Scan Steam library
            var steamPrograms = await ScanSteamLibrary();
            programs.AddRange(steamPrograms);

            // 3. Scan Epic Games
            var epicPrograms = await ScanEpicGamesLibrary();
            programs.AddRange(epicPrograms);

            // 4. Check compression status for each
            foreach (var program in programs)
            {
                try
                {
                    if (Directory.Exists(program.InstallPath))
                    {
                        program.IsCompressed = await _compressor.IsCompressedAsync(program.InstallPath);
                    }
                }
                catch
                {
                    // Ignore errors checking compression status
                }
            }
        });

        // Remove duplicates and sort by size
        return programs
            .GroupBy(p => p.InstallPath.ToLowerInvariant())
            .Select(g => g.First())
            .OrderByDescending(p => p.SizeBytes)
            .ToList();
    }

    private async Task<List<InstalledProgram>> ScanDirectory(string path, string type)
    {
        var programs = new List<InstalledProgram>();

        try
        {
            if (!Directory.Exists(path)) return programs;

            var directories = Directory.GetDirectories(path);
            foreach (var dir in directories)
            {
                try
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var size = await Task.Run(() => GetDirectorySize(dir));

                    // Only include directories > 100 MB
                    if (size > 100 * 1024 * 1024)
                    {
                        programs.Add(new InstalledProgram
                        {
                            Name = dirInfo.Name,
                            InstallPath = dir,
                            SizeBytes = size,
                            Type = type
                        });
                    }
                }
                catch
                {
                    // Skip directories we can't access
                }
            }
        }
        catch
        {
            // Ignore if we can't access the directory
        }

        return programs;
    }

    private async Task<List<InstalledProgram>> ScanSteamLibrary()
    {
        var programs = new List<InstalledProgram>();

        try
        {
            // Common Steam library locations
            var steamPaths = new[]
            {
                @"C:\Program Files (x86)\Steam\steamapps\common",
                @"C:\Program Files\Steam\steamapps\common",
                @"D:\SteamLibrary\steamapps\common",
                @"E:\SteamLibrary\steamapps\common"
            };

            foreach (var steamPath in steamPaths)
            {
                var steamGames = await ScanDirectory(steamPath, "Game");
                programs.AddRange(steamGames);
            }
        }
        catch
        {
            // Ignore Steam scan errors
        }

        return programs;
    }

    private async Task<List<InstalledProgram>> ScanEpicGamesLibrary()
    {
        var programs = new List<InstalledProgram>();

        try
        {
            var epicPath = @"C:\Program Files\Epic Games";
            if (Directory.Exists(epicPath))
            {
                var epicGames = await ScanDirectory(epicPath, "Game");
                programs.AddRange(epicGames);
            }
        }
        catch
        {
            // Ignore Epic scan errors
        }

        return programs;
    }

    private long GetDirectorySize(string path)
    {
        try
        {
            var dirInfo = new DirectoryInfo(path);
            return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(file => file.Length);
        }
        catch
        {
            return 0;
        }
    }
}
