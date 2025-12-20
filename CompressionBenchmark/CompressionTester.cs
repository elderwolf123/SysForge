using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RamOptimizer.Compression.HyperCompress;
using RamOptimizer.Compression.HyperCompress.Encoders;

namespace CompressionBenchmark;

/// <summary>
/// Tests all compression algorithms with all settings on discovered file types.
/// Outputs detailed results for learning and optimization.
/// </summary>
public class CompressionTester
{
    private readonly HyperCompressEngine _engine;
    private readonly FileTypeDatabase _database;
    private readonly RamSafetyChecker _ramChecker;

    public CompressionTester(FileTypeDatabase database)
    {
        _database = database;
        _ramChecker = new RamSafetyChecker();
        _engine = new HyperCompressEngine();
        
        // Register only available encoders
        _engine.RegisterEncoder(new HyperGeneralEncoder());
        _engine.RegisterEncoder(new FallbackLZ4Encoder());
        
        // Show RAM status
        _ramChecker.PrintRamStatus();
    }

    public void TestFileType(string extension)
    {
        var entry = _database.GetEntry(extension);
        if (entry == null || entry.SamplePaths.Count == 0)
        {
            Console.WriteLine($"⚠️  No sample files for {extension}");
            BenchmarkLogger.LogWarning($"No samples for extension: {extension}");
            return;
        }

        Console.WriteLine($"\n{new string('=', 60)}");
        Console.WriteLine($"TESTING: {extension}");
        Console.WriteLine($"Samples: {entry.SamplePaths.Count} files");
        Console.WriteLine($"{new string('=', 60)}\n");

        // Mark as in-progress for crash recovery
        _database.MarkAsInProgress(extension);
        BenchmarkLogger.LogInfo($"Starting test for {extension} with {entry.SamplePaths.Count} samples");

        // Group samples by size bracket
        var sizeBrackets = new Dictionary<string, List<string>>();
        foreach (var path in entry.SamplePaths)
        {
            if (!File.Exists(path)) continue;
            
            var size = new FileInfo(path).Length;
            var bracket = GetSizeBracket(size);
            
            if (!sizeBrackets.ContainsKey(bracket))
                sizeBrackets[bracket] = new List<string>();
            
            sizeBrackets[bracket].Add(path);
        }

        // Test MULTIPLE files from each size bracket (up to 3 per bracket)
        var allResults = new List<CompressionResults>();
        
        foreach (var bracket in sizeBrackets.OrderBy(b => GetBracketOrder(b.Key)))
        {
            Console.WriteLine($"📊 Size Bracket: {bracket.Key} ({bracket.Value.Count} files)");
            
            // Test up to 3 files per bracket for better data
            var samplesToTest = bracket.Value.Take(3).ToList();
            
            foreach (var samplePath in samplesToTest)
            {
                Console.WriteLine($"   Testing: {Path.GetFileName(samplePath)}");
                
                var result = TestSingleFile(samplePath, extension);
                if (result != null)
                {
                    allResults.Add(result);
                }
            }
            
            Console.WriteLine();
        }

        // Calculate overall average
        if (allResults.Count > 0)
        {
            var avgResults = CalculateAverageResults(allResults);
            
            Console.WriteLine($"📈 OVERALL AVERAGE (from {allResults.Count} files):");
            Console.WriteLine($"   Best: {avgResults.BestAlgorithm} - {avgResults.BestRatio:P1}");
            
            _database.MarkAsTested(extension, avgResults);
            BenchmarkLogger.LogInfo($"Completed {extension}: {allResults.Count} files tested, best: {avgResults.BestAlgorithm}");
        }
    }

    private CompressionResults? TestSingleFile(string filePath, string extension)
    {
        if (!File.Exists(filePath))
        {
            BenchmarkLogger.LogWarning($"File not found: {filePath}");
            return null;
        }

        var fileSize = new FileInfo(filePath).Length;
        
        // Check if file is safe to test
        if (!_ramChecker.CanTestFile(fileSize, out string reason))
        {
            Console.WriteLine($"   ⏭️  SKIPPED: {reason}");
            BenchmarkLogger.LogInfo($"Skipped {filePath}: {reason}");
            return null;
        }

        // SAFETY: Create temporary copy to avoid corrupting original
        string? tempCopy = null;
        byte[] originalData;
        
        try
        {
            tempCopy = Path.Combine(Path.GetTempPath(), $"benchmark_temp_{Guid.NewGuid()}{Path.GetExtension(filePath)}");
            File.Copy(filePath, tempCopy, overwrite: true);
            BenchmarkLogger.LogFileSafety("COPY_CREATED", tempCopy);
            
            originalData = File.ReadAllBytes(tempCopy);
            Console.WriteLine($"   Original Size: {FormatBytes(originalData.Length)}");
        }
        catch (OutOfMemoryException ex)
        {
            Console.WriteLine($"   ❌ Out of memory!");
            BenchmarkLogger.LogError($"OOM reading {filePath}", ex);
            CleanupTempFile(tempCopy);
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Failed to read file: {ex.Message}");
            BenchmarkLogger.LogError($"Failed to read {filePath}", ex);
            CleanupTempFile(tempCopy);
            return null;
        }

        var results = new CompressionResults
        {
            OriginalSize = originalData.Length,
            Results = new Dictionary<string, AlgorithmResult>()
        };

        // Test all algorithms with verification - NOW WITH PARALLEL SUPPORT
        var testTasks = new List<(string name, Func<byte[]> compressFunc)>();

        // Base algorithms
        testTasks.Add(("LZ4", () => 
        {
            var encoder = new FallbackLZ4Encoder();
            return encoder.Compress(originalData, new CompressionSettings());
        }));

        // Zstd levels
        foreach (var level in new[] { 1, 3, 6, 9, 12, 15, 19, 22 })
        {
            var currentLevel = level; // Capture for closure
            testTasks.Add(($"Zstd L{currentLevel}", () =>
            {
                var settings = new CompressionSettings { Level = currentLevel };
                return _engine.Compress(originalData, "test" + extension, settings);
            }));
        }

        // HyperGeneral
        testTasks.Add(("HyperGeneral", () =>
        {
            var encoder = new HyperGeneralEncoder();
            return encoder.Compress(originalData, new CompressionSettings());
        }));

        // Brotli levels
        testTasks.Add(("Brotli Optimal", () =>
        {
            var encoder = new BrotliEncoder();
            return encoder.Compress(originalData, (int)System.IO.Compression.CompressionLevel.Optimal);
        }));

        testTasks.Add(("Brotli Max", () =>
        {
            var encoder = new BrotliEncoder();
            return encoder.Compress(originalData, (int)System.IO.Compression.CompressionLevel.SmallestSize);
        }));

        // LZ4-HC levels
        foreach (var level in new[] { 9, 12 })
        {
            var currentLevel = level;
            testTasks.Add(($"LZ4-HC L{currentLevel}", () =>
            {
                var encoder = new LZ4HCEncoder();
                return encoder.Compress(originalData, currentLevel);
            }));
        }

        // Bzip2
        testTasks.Add(("Bzip2", () =>
        {
            var encoder = new Bzip2Encoder();
            return encoder.Compress(originalData);
        }));

        // Gzip
        testTasks.Add(("Gzip", () =>
        {
            var encoder = new GzipEncoder();
            return encoder.Compress(originalData, new CompressionSettings());
        }));

        // LZMA
        testTasks.Add(("LZMA", () =>
        {
            var encoder = new LZMAEncoder();
            return encoder.Compress(originalData, new CompressionSettings());
        }));

        // Execute tests with parallel support (for small/medium files)
        if (originalData.Length < 10 * 1024 * 1024) // <10MB: Use parallel
        {
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Math.Min(4, Environment.ProcessorCount) };
            
            Parallel.ForEach(testTasks, parallelOptions, task =>
            {
                TestAlgorithmWithVerification(task.name, originalData, results, extension, task.compressFunc);
            });
        }
        else // Large files: Sequential to avoid RAM issues
        {
            foreach (var task in testTasks)
            {
                TestAlgorithmWithVerification(task.name, originalData, results, extension, task.compressFunc);
            }
        }

        if (results.Results.Count > 0)
        {
            var best = results.Results.OrderBy(r => r.Value.CompressionRatio).First();
            results.BestAlgorithm = best.Key;
            results.BestRatio = best.Value.CompressionRatio;

            Console.WriteLine($"   🏆 Best: {best.Key} - {best.Value.CompressionRatio:P1}");
        }
        
        // Cleanup temp file
        CleanupTempFile(tempCopy);
        
        return results;
    }

    private void TestAlgorithmWithVerification(string name, byte[] originalData, CompressionResults results, 
                                               string extension, Func<byte[]> compressFunc)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var beforeMem = GC.GetTotalMemory(false);
            
            var compressed = compressFunc();
            
            sw.Stop();
            var afterMem = GC.GetTotalMemory(false);
            var memUsed = Math.Max(0, afterMem - beforeMem);

            // VERIFICATION: Test decompression to ensure data integrity
            try
            {
                if (compressed.Length == 0)
                {
                    throw new Exception("Compression resulted in zero-length output");
                }
                
                // Try to decompress with appropriate decoder
                if (name.Contains("Brotli"))
                {
                    var encoder = new BrotliEncoder();
                    var decompressed = encoder.Decompress(compressed);
                    if (decompressed.Length != originalData.Length)
                        throw new Exception($"Decompressed size mismatch: {decompressed.Length} vs {originalData.Length}");
                }
                else if (name == "Bzip2")
                {
                    var encoder = new Bzip2Encoder();
                    var decompressed = encoder.Decompress(compressed);
                    if (decompressed.Length != originalData.Length)
                        throw new Exception($"Decompressed size mismatch: {decompressed.Length} vs {originalData.Length}");
                }
                else if (name == "Gzip")
                {
                    var encoder = new GzipEncoder();
                    var decompressed = encoder.Decompress(compressed);
                    if (decompressed.Length != originalData.Length)
                        throw new Exception($"Decompressed size mismatch: {decompressed.Length} vs {originalData.Length}");
                }
                else if (name == "LZMA")
                {
                    var encoder = new LZMAEncoder();
                    var decompressed = encoder.Decompress(compressed);
                    if (decompressed.Length != originalData.Length)
                        throw new Exception($"Decompressed size mismatch: {decompressed.Length} vs {originalData.Length}");
                }
            }
            catch (Exception ex)
            {
                BenchmarkLogger.LogDecompressionFailure($"{extension}_sample", name, ex);
                lock (results) // Thread-safe console output
                {
                    Console.WriteLine($"  {name,-20} | ❌ VERIFICATION FAILED: {ex.Message}");
                }
                return;
            }

            var ratio = compressed.Length / (double)originalData.Length;
            var savings = (1 - ratio) * 100;

            var result = new AlgorithmResult
            {
                CompressedSize = compressed.Length,
                CompressionRatio = ratio,
                CompressionTimeMs = sw.ElapsedMilliseconds,
                MemoryUsedBytes = memUsed
            };

            lock (results.Results) // Thread-safe dictionary access
            {
                results.Results[name] = result;
            }

            lock (results) // Thread-safe console output
            {
                Console.WriteLine($"  {name,-20} | " +
                                $"Ratio: {ratio:P1} | " +
                                $"Size: {FormatBytes(compressed.Length),-10} | " +
                                $"Time: {sw.ElapsedMilliseconds,4}ms | " +
                                $"Savings: {savings:F1}%");
            }
        }
        catch (Exception ex)
        {
            lock (results) // Thread-safe console output
            {
                Console.WriteLine($"  {name,-20} | ❌ FAILED: {ex.Message}");
            }
            BenchmarkLogger.LogCompressionIssue($"{extension}_sample", name, ex.Message);
        }
    }

    private void CleanupTempFile(string? tempFile)
    {
        if (tempFile != null && File.Exists(tempFile))
        {
            try
            {
                File.Delete(tempFile);
                BenchmarkLogger.LogFileSafety("TEMP_DELETED", tempFile);
            }
            catch (Exception ex)
            {
                BenchmarkLogger.LogWarning($"Failed to delete temp file {tempFile}: {ex.Message}");
            }
        }
    }

    private CompressionResults CalculateAverageResults(List<CompressionResults> allResults)
    {
        var avgResults = new CompressionResults
        {
            OriginalSize = (long)allResults.Average(r => r.OriginalSize),
            Results = new Dictionary<string, AlgorithmResult>()
        };

        var allAlgos = allResults.SelectMany(r => r.Results.Keys).Distinct();
        foreach (var algo in allAlgos)
        {
            var algoResults = allResults.Where(r => r.Results.ContainsKey(algo)).Select(r => r.Results[algo]).ToList();
            if (algoResults.Count > 0)
            {
                avgResults.Results[algo] = new AlgorithmResult
                {
                    CompressedSize = (long)algoResults.Average(a => a.CompressedSize),
                    CompressionRatio = algoResults.Average(a => a.CompressionRatio),
                    CompressionTimeMs = (long)algoResults.Average(a => a.CompressionTimeMs),
                    MemoryUsedBytes = (long)algoResults.Average(a => a.MemoryUsedBytes)
                };
            }
        }

        var best = avgResults.Results.OrderBy(r => r.Value.CompressionRatio).First();
        avgResults.BestAlgorithm = best.Key;
        avgResults.BestRatio = best.Value.CompressionRatio;
        
        return avgResults;
    }

    private string GetSizeBracket(long size)
    {
        if (size < 100 * 1024) return "Tiny (<100KB)";
        if (size < 1024 * 1024) return "Small (<1MB)";
        if (size < 10 * 1024 * 1024) return "Medium (<10MB)";
        if (size < 100 * 1024 * 1024) return "Large (<100MB)";
        return "Huge (>100MB)";
    }

    private int GetBracketOrder(string bracket)
    {
        return bracket switch
        {
            "Tiny (<100KB)" => 0,
            "Small (<1MB)" => 1,
            "Medium (<10MB)" => 2,
            "Large (<100MB)" => 3,
            "Huge (>100MB)" => 4,
            _ => 5
        };
    }

    private void TestAlgorithm(string name, byte[] originalData, CompressionResults results, 
                              Func<byte[]> compressFunc)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var beforeMem = GC.GetTotalMemory(false);
            
            var compressed = compressFunc();
            
            sw.Stop();
            var afterMem = GC.GetTotalMemory(false);
            var memUsed = Math.Max(0, afterMem - beforeMem);

            var ratio = compressed.Length / (double)originalData.Length;
            var savings = (1 - ratio) * 100;

            var result = new AlgorithmResult
            {
                CompressedSize = compressed.Length,
                CompressionRatio = ratio,
                CompressionTimeMs = sw.ElapsedMilliseconds,
                MemoryUsedBytes = memUsed
            };

            results.Results[name] = result;

            // Output result
            Console.WriteLine($"  {name,-15} | " +
                            $"Ratio: {ratio:P1} | " +
                            $"Size: {FormatBytes(compressed.Length),-10} | " +
                            $"Time: {sw.ElapsedMilliseconds}ms | " +
                            $"Savings: {savings:F1}%");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  {name,-15} | ❌ FAILED: {ex.Message}");
        }
    }

    public void TestAllUntested()
    {
        var untested = _database.GetUntestedFileTypes();
        
        Console.WriteLine($"\n🎯 Found {untested.Count} untested file types\n");

        int tested = 0;
        foreach (var extension in untested)
        {
            tested++;
            Console.WriteLine($"\n[{tested}/{untested.Count}]");
            TestFileType(extension);
            
            // Save progress after each test
            _database.SaveDatabase();
        }

        Console.WriteLine($"\n✅ Testing complete! Tested {tested} file types");
    }

    public void RetryFailedTests()
    {
        var failedEntries = _database.GetEntriesWithStatus(TestStatus.Failed)
            .Where(e => e.SamplePaths.Any(p => File.Exists(p)))
            .ToList();
        
        Console.WriteLine($"\n🔄 Found {failedEntries.Count} file types with failed samples\n");

        int retried = 0;
        foreach (var entry in failedEntries)
        {
            retried++;
            Console.WriteLine($"\n[{retried}/{failedEntries.Count}] Retrying: {entry.Extension}");
            
            TestFileType(entry.Extension);
            
            // Save progress after each retry
            _database.SaveDatabase();
        }

        Console.WriteLine($"\n✅ Retry complete! Attempted to retry {retried} file types");
    }

    public void GenerateReport()
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("COMPRESSION BENCHMARK REPORT");
        Console.WriteLine(new string('=', 80));

        var allResults = _database.GetUntestedFileTypes()
            .Select(ext => _database.GetEntry(ext))
            .Where(e => e != null && e.Results != null)
            .ToList();

        if (allResults.Count == 0)
        {
            Console.WriteLine("No test results available yet.");
            return;
        }

        // Group by best algorithm
        var byBestAlgo = allResults
            .GroupBy(e => e!.Results!.BestAlgorithm)
            .OrderByDescending(g => g.Count());

        Console.WriteLine("\nBEST ALGORITHM BY FILE TYPE COUNT:");
        foreach (var group in byBestAlgo)
        {
            Console.WriteLine($"  {group.Key,-15}: {group.Count()} file types");
        }

        // Show compression champions
        Console.WriteLine("\nTOP 10 MOST COMPRESSIBLE FILE TYPES:");
        var topCompressible = allResults
            .OrderBy(e => e!.Results!.BestRatio)
            .Take(10);

        foreach (var entry in topCompressible)
        {
            var results = entry!.Results!;
            Console.WriteLine($"  {entry.Extension,-10} - {results.BestRatio:P1} " +
                            $"({results.BestAlgorithm})");
        }

        // Show compression failures
        Console.WriteLine("\nTOP 10 LEAST COMPRESSIBLE FILE TYPES:");
        var leastCompressible = allResults
            .OrderByDescending(e => e!.Results!.BestRatio)
            .Take(10);

        foreach (var entry in leastCompressible)
        {
            var results = entry!.Results!;
            Console.WriteLine($"  {entry.Extension,-10} - {results.BestRatio:P1} " +
                            $"({results.BestAlgorithm})");
        }

        Console.WriteLine("\n" + new string('=', 80));
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

    private static bool IsValidJsonNumber(double value)
    {
        return !double.IsInfinity(value) && !double.IsNaN(value) && !double.IsNegativeInfinity(value);
    }

    private static double SafeCompressionRatio(long compressed, long original)
    {
        if (original == 0) return 0;
        return (double)compressed / original;
    }
}
