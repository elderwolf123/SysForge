using System;
using System.IO;
using System.Linq;
using RamOptimizer.Compression.HyperCompress;
using RamOptimizer.Compression.HyperCompress.Encoders;

Console.WriteLine("=== HyperCompress System Test ===\n");

// Initialize
var engine = new HyperCompressEngine();
engine.RegisterEncoder(new FallbackLZ4Encoder());
engine.RegisterEncoder(new HyperGameTextureEncoder());
engine.RegisterEncoder(new HyperGameAudioEncoder());
engine.RegisterEncoder(new HyperGameExecutableEncoder());
engine.RegisterEncoder(new HyperGeneralEncoder());

var archiver = new ChunkedArchiver(engine);

Console.WriteLine("✅ Engine initialized with 5 encoders\n");

// Test 1: Individual Encoders
Console.WriteLine("[Test 1/3] Testing Individual Encoders");
Console.WriteLine("---------------------------------------");

try
{
    var generalEncoder = new HyperGeneralEncoder();
    var text = System.Text.Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("Hello World! ", 100)));
    var compressed = generalEncoder.Compress(text, new CompressionSettings { Level = 10 });
    var decompressed = generalEncoder.Decompress(compressed);
    bool match = text.SequenceEqual(decompressed);
    
    Console.WriteLine($"  General Encoder: {text.Length} → {compressed.Length} bytes ({(float)compressed.Length / text.Length:P2})");
    Console.WriteLine($"  Decompression match: {(match ? "✅ PASS" : "❌ FAIL")}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"  ❌ FAIL: {ex.Message}\n");
}

// Test 2: Archive Creation & Extraction
Console.WriteLine("[Test 2/3] Testing Archive System");
Console.WriteLine("---------------------------------------");

string testDir = Path.Combine(Path.GetTempPath(), "HCTest_" + Guid.NewGuid().ToString("N")[..8]);
string archivePath = Path.Combine(Path.GetTempPath(), "test.hca");
string extractDir = Path.Combine(Path.GetTempPath(), "HCExtract_" + Guid.NewGuid().ToString("N")[..8]);

try
{
    // Create test files
    Directory.CreateDirectory(testDir);
    File.WriteAllText(Path.Combine(testDir, "file1.txt"), "Test content 1");
    File.WriteAllText(Path.Combine(testDir, "file2.txt"), "Test content 2 with more data");
    
    var subDir = Path.Combine(testDir, "subdir");
    Directory.CreateDirectory(subDir);
    File.WriteAllText(Path.Combine(subDir, "file3.txt"), "Nested file content");
    
    Console.WriteLine($"  Created test files in {Path.GetFileName(testDir)}");
    
    // Create archive
    var task = archiver.CreateArchiveAsync(testDir, archivePath);
    task.Wait();
    var result = task.Result;
    
    if (!result.Success)
    {
        Console.WriteLine($"  ❌ Archive creation failed: {result.ErrorMessage}");
        return;
    }
    
    Console.WriteLine($"  ✅ Archive created: {result.TotalFiles} files");
    Console.WriteLine($"     Original: {result.OriginalSize} bytes");
    Console.WriteLine($"     Compressed: {result.CompressedSize} bytes");
    Console.WriteLine($"     Ratio: {result.CompressionRatio:P2}");
    
    // Extract archive
    Console.WriteLine($"\n  Attempting extraction...");
    
    using (var reader = new ChunkedArchiveReader(archivePath, engine))
    {
        reader.Open();
        var files = reader.GetFileList();
        Console.WriteLine($"  Archive contains {files.Count} files:");
        foreach (var file in files)
        {
            Console.WriteLine($"    - {file}");
        }
        
        reader.ExtractAll(extractDir);
        Console.WriteLine($"  ✅ Extraction complete!");
    }
    
    // Verify extracted files
    var file1Content = File.ReadAllText(Path.Combine(extractDir, "file1.txt"));
    var file3Content = File.ReadAllText(Path.Combine(extractDir, "subdir", "file3.txt"));
    
    bool verified = file1Content == "Test content 1" && file3Content == "Nested file content";
    Console.WriteLine($"  Content verification: {(verified ? "✅ PASS" : "❌ FAIL")}\n");
    
    // Cleanup
    Directory.Delete(testDir, true);
    Directory.Delete(extractDir, true);
    File.Delete(archivePath);
    
    Console.WriteLine("[Test 3/3] Cleanup Complete");
    Console.WriteLine("---------------------------------------");
    Console.WriteLine("  ✅ All test files cleaned up\n");
}
catch (Exception ex)
{
    Console.WriteLine($"  ❌ FAIL: {ex.Message}");
    Console.WriteLine($"  Type: {ex.GetType().Name}");
    Console.WriteLine($"  Stack: {ex.StackTrace}\n");
    
    if (ex.InnerException != null)
    {
        Console.WriteLine($"  Inner: {ex.InnerException.Message}");
    }
    
    // Cleanup on error
    if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
    if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
    if (File.Exists(archivePath)) File.Delete(archivePath);
}

Console.WriteLine("\n=== Test Complete ===");
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
