using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RamOptimizer.Compression.HyperCompress;
using RamOptimizer.Compression.HyperCompress.Encoders;

namespace RamOptimizer.Compression.Tests;

/// <summary>
/// Automated tests for HyperCompress Tier 3 system.
/// </summary>
[TestClass]
public class HyperCompressAutomatedTests
{
    private string _testDataPath = null!;
    private HyperCompressEngine _engine = null!;
    private ChunkedArchiver _archiver = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _testDataPath = Path.Combine(Path.GetTempPath(), "HyperCompressTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataPath);
        
        // Initialize engine with all encoders
        _engine = new HyperCompressEngine();
        _engine.RegisterEncoder(new FallbackLZ4Encoder());
        _engine.RegisterEncoder(new HyperGameTextureEncoder());
        _engine.RegisterEncoder(new HyperGameAudioEncoder());
        _engine.RegisterEncoder(new HyperGameExecutableEncoder());
        _engine.RegisterEncoder(new HyperGeneralEncoder());
        
        _archiver = new ChunkedArchiver(_engine);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, true);
        }
    }
    
    [TestMethod]
    public void Test01_TextureEncoder_DDS()
    {
        var encoder = new HyperGameTextureEncoder();
        
        // Create fake DDS file (with magic bytes)
        var ddsData = new byte[1024];
        ddsData[0] = 0x44; // 'D'
        ddsData[1] = 0x44; // 'D'
        ddsData[2] = 0x53; // 'S'
        ddsData[3] = 0x20; // ' '
        Random.Shared.NextBytes(ddsData.AsSpan(128)); // Fill with random pixel data
        
        Assert.IsTrue(encoder.IsSuitable(ddsData, "texture.dds"));
        
        var compressed = encoder.Compress(ddsData, new CompressionSettings { Level = 10 });
        Assert.IsTrue(compressed.Length < ddsData.Length, "DDS should compress");
        
        var decompressed = encoder.Decompress(compressed);
        Assert.AreEqual(ddsData.Length, decompressed.Length, "Decompressed size should match");
    }
    
    [TestMethod]
    public void Test02_AudioEncoder_WAV()
    {
        var encoder = new HyperGameAudioEncoder();
        
        // Create fake WAV file (with RIFF header)
        var wavData = new byte[2048];
        wavData[0] = 0x52; // 'R'
        wavData[1] = 0x49; // 'I'
        wavData[2] = 0x46; // 'F'
        wavData[3] = 0x46; // 'F'
        Random.Shared.NextBytes(wavData.AsSpan(44)); // PCM data after header
        
        Assert.IsTrue(encoder.IsSuitable(wavData, "audio.wav"));
        
        var compressed = encoder.Compress(wavData, new CompressionSettings { Level = 10 });
        Assert.IsTrue(compressed.Length < wavData.Length, "WAV should compress with delta encoding");
    }
    
    [TestMethod]
    public void Test03_ExecutableEncoder()
    {
        var encoder = new HyperGameExecutableEncoder();
        
        // Create fake EXE (with MZ header)
        var exeData = new byte[4096];
        exeData[0] = 0x4D; // 'M'
        exeData[1] = 0x5A; // 'Z'
        Random.Shared.NextBytes(exeData.AsSpan(64)); // Code section
        
        Assert.IsTrue(encoder.IsSuitable(exeData, "game.exe"));
        
        var compressed = encoder.Compress(exeData, new CompressionSettings { Level = 15 });
        Assert.IsTrue(compressed.Length < exeData.Length, "EXE should compress");
    }
    
    [TestMethod]
    public void Test04_GeneralEncoder_Text()
    {
        var encoder = new HyperGeneralEncoder();
        
        // Create text data
        var textData = System.Text.Encoding.UTF8.GetBytes(
            "This is a test file with lots of repetitive text. " +
            "This is a test file with lots of repetitive text. " +
            "This is a test file with lots of repetitive text.");
        
        Assert.IsTrue(encoder.IsSuitable(textData, "file.txt"));
        
        var compressed = encoder.Compress(textData, new CompressionSettings { Level = 10 });
        Assert.IsTrue(compressed.Length < textData.Length, "Text should compress well");
        
        var decompressed = encoder.Decompress(compressed);
        CollectionAssert.AreEqual(textData, decompressed, "Decompressed text should match original");
    }
    
    [TestMethod]
    public async Task Test05_ChunkedArchiver_CreateAndExtract()
    {
        // Create test directory with files
        var sourceDir = Path.Combine(_testDataPath, "source");
        Directory.CreateDirectory(sourceDir);
        
        File.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "Test content 1");
        File.WriteAllText(Path.Combine(sourceDir, "file2.txt"), "Test content 2 with more data");
        
        var subDir = Path.Combine(sourceDir, "subdir");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "file3.txt"), "Nested file content");
        
        // Create archive
        var archivePath = Path.Combine(_testDataPath, "test.hca");
        var result = await _archiver.CreateArchiveAsync(sourceDir, archivePath);
        
        Assert.IsTrue(result.Success, "Archive creation should succeed");
        Assert.AreEqual(3, result.TotalFiles, "Should have 3 files");
        Assert.IsTrue(File.Exists(archivePath), "Archive file should exist");
        Assert.IsTrue(result.CompressedSize < result.OriginalSize, "Should achieve compression");
        
        // Extract archive
        var extractDir = Path.Combine(_testDataPath, "extracted");
        var reader = new ChunkedArchiveReader(archivePath, _engine);
        reader.Open();
        
        var files = reader.GetFileList();
        Assert.AreEqual(3, files.Count, "Archive should contain 3 files");
        
        reader.ExtractAll(extractDir);
        reader.Dispose();
        
        // Verify extracted files
        Assert.IsTrue(File.Exists(Path.Combine(extractDir, "file1.txt")));
        Assert.IsTrue(File.Exists(Path.Combine(extractDir, "file2.txt")));
        Assert.IsTrue(File.Exists(Path.Combine(extractDir, "subdir", "file3.txt")));
        
        Assert.AreEqual("Test content 1", File.ReadAllText(Path.Combine(extractDir, "file1.txt")));
        Assert.AreEqual("Nested file content", File.ReadAllText(Path.Combine(extractDir, "subdir", "file3.txt")));
    }
    
    [TestMethod]
    public async Task Test06_LearningDatabase()
    {
        var dbPath = Path.Combine(_testDataPath, "learning.json");
        var db = new HyperCompressLearningDatabase(dbPath);
        
        // Learn from compression results
        var testData = new byte[1000];
        Random.Shared.NextBytes(testData);
        
        db.LearnFromCompression("test.bin", testData, HyperAlgorithm.HyperGeneral_Binary, 0.55f);
        db.LearnFromCompression("test.bin", testData, HyperAlgorithm.HyperGeneral_Binary, 0.52f);
        db.LearnFromCompression("test.bin", testData, HyperAlgorithm.HyperGeneral_Binary, 0.58f);
        
        // Database should have learned the pattern
        var stats = db.GetStatistics();
        Assert.IsTrue(stats.TotalPatterns > 0);
        Assert.IsTrue(stats.TotalObservations >= 3);
        
        // Should suggest best algorithm
        var bestAlgo = db.GetBestAlgorithm("test.bin", testData);
        Assert.IsNotNull(bestAlgo);
    }
    
    [TestMethod]
    public async Task Test07_PatternDetector()
    {
        var detector = new PatternDetector();
        
        // High entropy data
        var randomData = new byte[1024];
        Random.Shared.NextBytes(randomData);
        var pattern1 = detector.DetectPattern(randomData, "data.bin");
        Assert.IsTrue(pattern1.Entropy > 7.0f, "Random data should have high entropy");
        
        // Text data
        var textData = System.Text.Encoding.UTF8.GetBytes(new string('A', 1000));
        var pattern2 = detector.DetectPattern(textData, "file.txt");
        Assert.IsTrue(pattern2.Entropy < 5.0f, "Repetitive text should have low entropy");
        Assert.IsTrue(pattern2.Repetition > 0.5f, "Should detect high repetition");
    }
    
    [TestMethod]
    public async Task Test08_ArchiveFormat_HeaderValidation()
    {
        // Create a test archive
        var sourceDir = Path.Combine(_testDataPath, "source2");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "test.txt"), "Content");
        
        var archivePath = Path.Combine(_testDataPath, "format_test.hca");
        await _archiver.CreateArchiveAsync(sourceDir, archivePath);
        
        // Read and validate header
        using var fs = new FileStream(archivePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fs);
        
        var header = ArchiveFormat.Header.ReadFrom(reader);
        
        Assert.IsTrue(header.IsValid(), "Header should be valid");
        CollectionAssert.AreEqual(ArchiveFormat.MagicBytes, header.Magic, "Magic bytes should match");
        Assert.AreEqual(ArchiveFormat.CurrentVersion, header.Version, "Version should match");
        Assert.IsTrue(header.TotalChunks > 0, "Should have at least one chunk");
    }
    
    [TestMethod]
    public void Test09_CompressionSettings()
    {
        var settings = new CompressionSettings
        {
            Level = 15,
            MaxMemoryMB = 1024,
            EnableMultiThreading = true
        };
        
        Assert.AreEqual(15, settings.Level);
        Assert.AreEqual(1024, settings.MaxMemoryMB);
        Assert.IsTrue(settings.EnableMultiThreading);
    }
    
    [TestMethod]
    public async Task Test10_EndToEnd_RealGameSimulation()
    {
        // Simulate a small game folder structure
        var gameDir = Path.Combine(_testDataPath, "FakeGame");
        Directory.CreateDirectory(gameDir);
        
        // Create fake game files
        var texturesDir = Path.Combine(gameDir, "Textures");
        Directory.CreateDirectory(texturesDir);
        CreateFakeDDS(Path.Combine(texturesDir, "character.dds"), 4096);
        CreateFakeDDS(Path.Combine(texturesDir, "environment.dds"), 8192);
        
        var audioDir = Path.Combine(gameDir, "Audio");
        Directory.CreateDirectory(audioDir);
        CreateFakeWAV(Path.Combine(audioDir, "music.wav"), 16384);
        
        CreateFakeEXE(Path.Combine(gameDir, "Game.exe"), 32768);
        File.WriteAllText(Path.Combine(gameDir, "config.ini"), "[Settings]\nResolution=1920x1080\n");
        
        // Compress
        var archivePath = Path.Combine(_testDataPath, "FakeGame.hca");
        var result = await _archiver.CreateArchiveAsync(gameDir, archivePath, new CompressionSettings { Level = 15 });
        
        Assert.IsTrue(result.Success);
        Assert.AreEqual(5, result.TotalFiles);
        
        double compressionRatio = (double)result.CompressedSize / result.OriginalSize;
        Assert.IsTrue(compressionRatio < 0.8, $"Should achieve <80% compression, got {compressionRatio:P2}");
        
        // Extract and verify
        var extractDir = Path.Combine(_testDataPath, "FakeGame_Extracted");
        var reader = new ChunkedArchiveReader(archivePath, _engine);
        reader.Open();
        reader.ExtractAll(extractDir);
        reader.Dispose();
        
        // Verify structure
        Assert.IsTrue(Directory.Exists(Path.Combine(extractDir, "Textures")));
        Assert.IsTrue(Directory.Exists(Path.Combine(extractDir, "Audio")));
        Assert.IsTrue(File.Exists(Path.Combine(extractDir, "Game.exe")));
        
        Console.WriteLine($"✅ End-to-end test passed: {result.OriginalSize:N0} → {result.CompressedSize:N0} bytes ({compressionRatio:P2})");
    }
    
    // Helper methods
    private void CreateFakeDDS(string path, int size)
    {
        var data = new byte[size];
        data[0] = 0x44; data[1] = 0x44; data[2] = 0x53; data[3] = 0x20; // DDS magic
        Random.Shared.NextBytes(data.AsSpan(128));
        File.WriteAllBytes(path, data);
    }
    
    private void CreateFakeWAV(string path, int size)
    {
        var data = new byte[size];
        data[0] = 0x52; data[1] = 0x49; data[2] = 0x46; data[3] = 0x46; // RIFF
        Random.Shared.NextBytes(data.AsSpan(44));
        File.WriteAllBytes(path, data);
    }
    
    private void CreateFakeEXE(string path, int size)
    {
        var data = new byte[size];
        data[0] = 0x4D; data[1] = 0x5A; // MZ
        Random.Shared.NextBytes(data.AsSpan(64));
        File.WriteAllBytes(path, data);
    }
}
