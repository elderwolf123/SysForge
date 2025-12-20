using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CompressionBenchmark;

/// <summary>
/// Exports comprehensive benchmark results to multiple formats.
/// Creates baseline compression profiles for adaptive learning.
/// </summary>
public class ReportExporter
{
    private readonly FileTypeDatabase _database;

    public ReportExporter(FileTypeDatabase database)
    {
        _database = database;
    }

    public void ExportAll()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
        Console.WriteLine("\n📤 Exporting reports...");
        
        ExportToJson($"benchmark_results_{timestamp}.json");
        ExportToCsv($"benchmark_results_{timestamp}.csv");
        ExportToReadableText($"benchmark_report_{timestamp}.txt");
        ExportBaselineProfile($"compression_baseline_{timestamp}.json");
        
        Console.WriteLine("\n✅ All reports exported!");
    }

    private void ExportToJson(string filename)
    {
        var allResults = CollectAllResults();
        
        var export = new
        {
            GeneratedAt = DateTime.Now,
            TotalFileTypes = allResults.Count,
            SystemInfo = new
            {
                OS = Environment.OSVersion.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                MachineName = Environment.MachineName
            },
            Results = allResults.Select(r => new
            {
                r.Extension,
                r.FirstSeen,
                r.LastTested,
                r.TestCount,
                SampleCount = r.SamplePaths.Count,
                Results = r.Results,
                SizeBracketResults = r.SizeBracketResults
            })
        };

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        
        File.WriteAllText(filename, json);
        Console.WriteLine($"  ✓ JSON: {filename}");
    }

    private void ExportToCsv(string filename)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Extension,BestAlgorithm,BestRatio,OriginalSize,BestCompressedSize,SavingsPercent,CompressionTimeMs");

        var allResults = CollectAllResults();
        
        foreach (var entry in allResults.OrderBy(e => e.Results?.BestRatio ?? 1.0))
        {
            if (entry.Results == null) continue;

            var best = entry.Results.Results[entry.Results.BestAlgorithm];
            var savings = (1 - entry.Results.BestRatio) * 100;

            csv.AppendLine($"{entry.Extension}," +
                          $"{entry.Results.BestAlgorithm}," +
                          $"{entry.Results.BestRatio:F4}," +
                          $"{entry.Results.OriginalSize}," +
                          $"{best.CompressedSize}," +
                          $"{savings:F2}," +
                          $"{best.CompressionTimeMs}");
        }

        File.WriteAllText(filename, csv.ToString());
        Console.WriteLine($"  ✓ CSV: {filename}");
    }

    private void ExportToReadableText(string filename)
    {
        var report = new StringBuilder();
        
        report.AppendLine("╔══════════════════════════════════════════════════════════════╗");
        report.AppendLine("║    COMPRESSION BENCHMARK RESULTS - COMPREHENSIVE REPORT     ║");
        report.AppendLine("╚══════════════════════════════════════════════════════════════╝");
        report.AppendLine();
        report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"System: {Environment.MachineName} ({Environment.ProcessorCount} cores)");
        report.AppendLine($"OS: {Environment.OSVersion}");
        report.AppendLine();

        var allResults = CollectAllResults();
        
        report.AppendLine($"Total File Types Tested: {allResults.Count}");
        report.AppendLine(new string('=', 80));
        report.AppendLine();

        // Summary statistics
        var avgRatio = allResults.Average(r => r.Results?.BestRatio ?? 1.0);
        var totalOriginal = allResults.Sum(r => r.Results?.OriginalSize ?? 0);
        var totalCompressed = allResults.Sum(r => r.Results?.Results[r.Results.BestAlgorithm].CompressedSize ?? 0);
        
        report.AppendLine("SUMMARY STATISTICS");
        report.AppendLine(new string('-', 80));
        report.AppendLine($"Average Compression Ratio: {avgRatio:P2}");
        report.AppendLine($"Total Original Size: {FormatBytes(totalOriginal)}");
        report.AppendLine($"Total Compressed Size: {FormatBytes(totalCompressed)}");
        report.AppendLine($"Total Space Saved: {FormatBytes(totalOriginal - totalCompressed)} ({(1 - (totalCompressed / (double)totalOriginal)) * 100:F1}%)");
        report.AppendLine();

        // Best algorithms
        var byAlgorithm = allResults
            .Where(r => r.Results != null)
            .GroupBy(r => r.Results!.BestAlgorithm)
            .OrderByDescending(g => g.Count());

        report.AppendLine("BEST ALGORITHM BY FILE TYPE COUNT");
        report.AppendLine(new string('-', 80));
        foreach (var group in byAlgorithm)
        {
            report.AppendLine($"  {group.Key,-20}: {group.Count(),3} file types ({(group.Count() / (double)allResults.Count * 100):F1}%)");
        }
        report.AppendLine();

        // Top compressible
        report.AppendLine("TOP 20 MOST COMPRESSIBLE FILE TYPES");
        report.AppendLine(new string('-', 80));
        report.AppendLine("Extension    | Best Algo         | Ratio   | Orig Size  | Compressed | Savings");
        report.AppendLine(new string('-', 80));
        
        foreach (var entry in allResults.OrderBy(r => r.Results?.BestRatio ?? 1.0).Take(20))
        {
            if (entry.Results == null) continue;
            var best = entry.Results.Results[entry.Results.BestAlgorithm];
            var savings = (1 - entry.Results.BestRatio) * 100;
            
            report.AppendLine($"{entry.Extension,-12} | {entry.Results.BestAlgorithm,-17} | {entry.Results.BestRatio:P1}  | " +
                            $"{FormatBytes(entry.Results.OriginalSize),-10} | {FormatBytes(best.CompressedSize),-10} | {savings:F1}%");
        }
        report.AppendLine();

        // Least compressible
        report.AppendLine("TOP 20 LEAST COMPRESSIBLE FILE TYPES (Already Compressed)");
        report.AppendLine(new string('-', 80));
        report.AppendLine("Extension    | Best Algo         | Ratio   | Note");
        report.AppendLine(new string('-', 80));
        
        foreach (var entry in allResults.OrderByDescending(r => r.Results?.BestRatio ?? 0.0).Take(20))
        {
            if (entry.Results == null) continue;
            var note = entry.Results.BestRatio > 0.95 ? "Skip compression" : "Marginal gain";
            
            report.AppendLine($"{entry.Extension,-12} | {entry.Results.BestAlgorithm,-17} | {entry.Results.BestRatio:P1}  | {note}");
        }
        report.AppendLine();

        // Recommendations
        report.AppendLine("COMPRESSION RECOMMENDATIONS");
        report.AppendLine(new string('=', 80));
        report.AppendLine();
        report.AppendLine("For YOUR system:");
        report.AppendLine("  - Use HyperGeneral for: text files, logs, source code");
        report.AppendLine("  - Use Zstd L19 for: structured data (JSON, XML)");
        report.AppendLine("  - Use LZ4 for: fast decompression scenarios");
        report.AppendLine("  - Skip compression for: images, videos, already-compressed archives");
        report.AppendLine();
        report.AppendLine("Game Archive Findings:");
        var gameArchives = allResults.Where(r => r.Extension.Contains("pak") || 
                                                 r.Extension.Contains("assets") || 
                                                 r.Extension.Contains("bundle")).ToList();
        if (gameArchives.Any())
        {
            foreach (var archive in gameArchives)
            {
                if (archive.Results == null) continue;
                var potential = (1 - archive.Results.BestRatio) * 100;
                report.AppendLine($"  {archive.Extension}: {potential:F1}% potential savings (recompress candidate!)");
            }
        }
        else
        {
            report.AppendLine("  No game archives found in scan");
        }

        File.WriteAllText(filename, report.ToString());
        Console.WriteLine($"  ✓ Text Report: {filename}");
        
        // Also print to console
        Console.WriteLine("\n" + report.ToString());
    }

    private void ExportBaselineProfile(string filename)
    {
        var allResults = CollectAllResults();
        
        var baseline = new
        {
            GeneratedAt = DateTime.Now,
            SystemProfile = new
            {
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                OS = Environment.OSVersion.ToString()
            },
            CompressionRules = allResults
                .Where(r => r.Results != null)
                .Select(r => new
                {
                    FileExtension = r.Extension,
                    RecommendedAlgorithm = r.Results!.BestAlgorithm,
                    ExpectedRatio = r.Results.BestRatio,
                    ShouldCompress = r.Results.BestRatio < 0.95, // Only if >5% savings
                    Priority = r.Results.BestRatio < 0.50 ? "High" :
                              r.Results.BestRatio < 0.75 ? "Medium" : "Low"
                })
                .OrderBy(r => r.ExpectedRatio),
            Statistics = new
            {
                TotalFileTypesTested = allResults.Count,
                CompressibleTypes = allResults.Count(r => r.Results?.BestRatio < 0.95),
                AverageCompressionRatio = allResults.Average(r => r.Results?.BestRatio ?? 1.0)
            }
        };

        var json = JsonSerializer.Serialize(baseline, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        
        File.WriteAllText(filename, json);
        Console.WriteLine($"  ✓ Baseline Profile: {filename}");
        Console.WriteLine($"\n💡 Use this baseline in your compression engine for adaptive selection!");
    }

    private List<FileTypeEntry> CollectAllResults()
    {
        return _database.GetUntestedFileTypes()
            .Select(ext => _database.GetEntry(ext))
            .Where(e => e != null && e.Results != null)
            .Cast<FileTypeEntry>()
            .ToList();
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
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
