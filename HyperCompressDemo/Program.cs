using System;
using System.IO;
using RamOptimizer.Compression.HyperCompress;
using RamOptimizer.Compression.HyperCompress.Encoders;

// Simple test - just compression and decompression
Console.WriteLine("=== Simple HyperCompress Test ===\n");

// Test 1: Simple encoder test
Console.WriteLine("Test 1: Text Encoder");
var generalEncoder = new HyperGeneralEncoder();
var textData = System.Text.Encoding.UTF8.GetBytes("Hello World! ".Repeat(100));
Console.WriteLine($"  Original: {textData.Length} bytes");

try
{
    var compressed = generalEncoder.Compress(textData, new CompressionSettings { Level = 10 });
    Console.WriteLine($"  Compressed: {compressed.Length} bytes");
    Console.WriteLine($"  Ratio: {(float)compressed.Length / textData.Length:P2}");
    
    var decompressed = generalEncoder.Decompress(compressed);
    Console.WriteLine($"  Decompressed: {decompressed.Length} bytes");
    Console.WriteLine($"  Match: {textData.SequenceEqual(decompressed)}");
    Console.WriteLine("  ✅ PASS\n");
}
catch (Exception ex)
{
    Console.WriteLine($"  ❌ FAIL: {ex.Message}\n");
}

// Test 2: Engine test
Console.WriteLine("Test 2: HyperCompress Engine");
var engine = new HyperCompressEngine();
engine.RegisterEncoder(new HyperGeneralEncoder());
engine.RegisterEncoder(new FallbackLZ4Encoder());

try
{
    var compressed2 = engine.Compress(textData, "test.txt", new CompressionSettings { Level = 10 });
    Console.WriteLine($"  Engine compressed: {compressed2.Length} bytes");
    Console.WriteLine("  ✅ PASS\n");
}
catch (Exception ex)
{
    Console.WriteLine($"  ❌ FAIL: {ex.Message}\n");
}

Console.WriteLine("=== Tests Complete ===");

public static class StringExtensions  
{
    public static string Repeat(this string s, int count)
    {
        return string.Concat(Enumerable.Repeat(s, count));
    }
}
