using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using RamOptimizer.Compression.HyperCompress;
using RamOptimizer.Compression.HyperCompress.Encoders;

namespace HyperCompressDemo;

public partial class MainWindow : Window
{
    private readonly StringBuilder _logBuilder = new();
    private HyperCompressEngine? _engine;
    private ChunkedArchiver? _archiver;
    
    public MainWindow()
    {
        InitializeComponent();
        Log("=== HyperCompress Test Suite Initialized ===", "#4CAF50");
        Log("Ready to test compression system\n", "#888");
        InitializeEngine();
    }
    
    private void InitializeEngine()
    {
        try
        {
            _engine = new HyperCompressEngine();
            _engine.RegisterEncoder(new FallbackLZ4Encoder());
            _engine.RegisterEncoder(new HyperGameTextureEncoder());
            _engine.RegisterEncoder(new HyperGameAudioEncoder());
            _engine.RegisterEncoder(new HyperGameExecutableEncoder());
            _engine.RegisterEncoder(new HyperGeneralEncoder());
            
            _archiver = new ChunkedArchiver(_engine);
            
            Log("✅ Engine initialized with 5 encoders", "#4CAF50");
        }
        catch (Exception ex)
        {
            Log($"❌ Engine initialization failed: {ex.Message}", "#F44336");
            Log($"Stack: {ex.StackTrace}", "#888");
        }
    }
    
    private async void TestEncoders_Click(object sender, RoutedEventArgs e)
    {
        SetStatus("Testing encoders...", "#2196F3");
        TestEncodersButton.IsEnabled = false;
        
        await Task.Run(() =>
        {
            try
            {
                LogOnUI("\n=== ENCODER TESTS ===\n", "#FFA500");
                
                // Test 1: General Encoder
                TestGeneralEncoder();
                
                // Test 2: Texture Encoder
                TestTextureEncoder();
                
                // Test 3: Audio Encoder
                TestAudioEncoder();
                
                // Test 4: Executable Encoder
                TestExecutableEncoder();
                
                LogOnUI("\n✅ All encoder tests passed!\n", "#4CAF50");
                SetStatusOnUI("Encoder tests complete", "#4CAF50");
            }
            catch (Exception ex)
            {
                LogOnUI($"\n❌ Encoder test failed: {ex.Message}", "#F44336");
                LogOnUI($"Stack: {ex.StackTrace}", "#888");
                SetStatusOnUI("Encoder tests failed", "#F44336");
            }
            finally
            {
                Dispatcher.Invoke(() => TestEncodersButton.IsEnabled = true);
            }
        });
    }
    
    private async void TestArchive_Click(object sender, RoutedEventArgs e)
    {
        SetStatus("Testing archive system...", "#2196F3");
        TestArchiveButton.IsEnabled = false;
        
        await Task.Run(() =>
        {
            try
            {
                LogOnUI("\n=== ARCHIVE SYSTEM TEST ===\n", "#FFA500");
                TestArchiveSystem();
                LogOnUI("\n✅ Archive test passed!\n", "#4CAF50");
                SetStatusOnUI("Archive test complete", "#4CAF50");
            }
            catch (Exception ex)
            {
                LogOnUI($"\n❌ Archive test failed: {ex.Message}", "#F44336");
                LogOnUI($"Type: {ex.GetType().Name}", "#888");
                LogOnUI($"Stack: {ex.StackTrace}", "#888");
                if (ex.InnerException != null)
                {
                    LogOnUI($"Inner: {ex.InnerException.Message}", "#888");
                }
                SetStatusOnUI("Archive test failed", "#F44336");
            }
            finally
            {
                Dispatcher.Invoke(() => TestArchiveButton.IsEnabled = true);
            }
        });
    }
    
    private void TestGeneralEncoder()
    {
        Log("\n[Test 1] General Encoder (Text)", "#2196F3");
        var encoder = new HyperGeneralEncoder();
        var textData = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("Hello World! ", 100)));
        
        Log($"  Original: {textData.Length} bytes");
        var compressed = encoder.Compress(textData, new CompressionSettings { Level = 10 });
        Log($"  Compressed: {compressed.Length} bytes ({(float)compressed.Length / textData.Length:P2})");
        
        var decompressed = encoder.Decompress(compressed);
        bool match = textData.SequenceEqual(decompressed);
        Log($"  Decompressed: {decompressed.Length} bytes - Match: {match}", match ? "#4CAF50" : "#F44336");
        
        if (!match) throw new Exception("Decompression mismatch!");
    }
    
    private void TestTextureEncoder()
    {
        Log("\n[Test 2] Texture Encoder (DDS)", "#2196F3");
        var encoder = new HyperGameTextureEncoder();
        var ddsData = new byte[4096];
        ddsData[0] = 0x44; ddsData[1] = 0x44; ddsData[2] = 0x53; ddsData[3] = 0x20; // DDS magic
        Random.Shared.NextBytes(ddsData.AsSpan(128));
        
        Log($"  Original: {ddsData.Length} bytes");
        var compressed = encoder.Compress(ddsData, new CompressionSettings { Level = 10 });
        Log($"  Compressed: {compressed.Length} bytes ({(float)compressed.Length / ddsData.Length:P2})");
        
        var decompressed = encoder.Decompress(compressed);
        bool match = ddsData.SequenceEqual(decompressed);
        Log($"  Decompressed: {decompressed.Length} bytes - Match: {match}", match ? "#4CAF50" : "#F44336");
        
        if (!match) throw new Exception("Decompression mismatch!");
    }
    
    private void TestAudioEncoder()
    {
        Log("\n[Test 3] Audio Encoder (WAV)", "#2196F3");
        var encoder = new HyperGameAudioEncoder();
        var wavData = new byte[8192];
        wavData[0] = 0x52; wavData[1] = 0x49; wavData[2] = 0x46; wavData[3] = 0x46; // RIFF
        Random.Shared.NextBytes(wavData.AsSpan(44));
        
        Log($"  Original: {wavData.Length} bytes");
        var compressed = encoder.Compress(wavData, new CompressionSettings { Level = 10 });
        Log($"  Compressed: {compressed.Length} bytes ({(float)compressed.Length / wavData.Length:P2})");
        Log("  ✅ WAV compression successful", "#4CAF50");
    }
    
    private void TestExecutableEncoder()
    {
        Log("\n[Test 4] Executable Encoder (EXE)", "#2196F3");
        var encoder = new HyperGameExecutableEncoder();
        var exeData = new byte[16384];
        exeData[0] = 0x4D; exeData[1] = 0x5A; // MZ
        Random.Shared.NextBytes(exeData.AsSpan(64));
        
        Log($"  Original: {exeData.Length} bytes");
        var compressed = encoder.Compress(exeData, new CompressionSettings { Level = 15 });
        Log($"  Compressed: {compressed.Length} bytes ({(float)compressed.Length / exeData.Length:P2})");
        Log("  ✅ EXE compression successful", "#4CAF50");
    }
    
    private void TestArchiveSystem()
    {
        Log("Creating test files...");
        
        string testDir = Path.Combine(Path.GetTempPath(), "HCTest_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(testDir);
        
        try
        {
            // Create test files
            File.WriteAllText(Path.Combine(testDir, "file1.txt"), "Test content 1");
            File.WriteAllText(Path.Combine(testDir, "file2.txt"), "Test content 2 with more data for compression");
            Log($"  Created test directory: {testDir}");
            
            // Create archive
            string archivePath = Path.Combine(Path.GetTempPath(), "test.hca");
            Log($"\nCompressing to {Path.GetFileName(archivePath)}...");
            
            var task = _archiver!.CreateArchiveAsync(testDir, archivePath);
            task.Wait();
            var result = task.Result;
            
            if (!result.Success)
            {
                throw new Exception($"Archive creation failed: {result.ErrorMessage}");
            }
            
            Log($"  ✅ Archive created");
            Log($"  Files: {result.TotalFiles}");
            Log($"  Original: {result.OriginalSize} bytes");
            Log($"  Compressed: {result.CompressedSize} bytes");
            Log($"  Ratio: {result.CompressionRatio:P2}");
            
            // Try to extract
            Log("\nAttempting extraction...");
            string extractDir = Path.Combine(Path.GetTempPath(), "HCExtract_" + Guid.NewGuid().ToString("N")[..8]);
            
            using (var reader = new ChunkedArchiveReader(archivePath, _engine!))
            {
                reader.Open();
                Log($"  Archive opened, contains {reader.GetFileList().Count} files");
                
                reader.ExtractAll(extractDir);
                Log($"  ✅ Extraction complete to {extractDir}");
            }
            
            // Cleanup
            Directory.Delete(testDir, true);
            Directory.Delete(extractDir, true);
            File.Delete(archivePath);
            Log("  Cleanup complete");
        }
        catch (Exception ex)
        {
            if (Directory.Exists(testDir))
                Directory.Delete(testDir, true);
            throw;
        }
    }
    
    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        _logBuilder.Clear();
        LogTextBlock.Inlines.Clear();
        SetStatus("Log cleared", "#888");
    }
    
    private void CopyLog_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(_logBuilder.ToString());
        SetStatus("Log copied to clipboard!", "#4CAF50");
    }
    
    private void Log(string message, string color = "#E0E0E0")
    {
        Dispatcher.Invoke(() =>
        {
            _logBuilder.AppendLine(message);
            var run = new System.Windows.Documents.Run(message + "\n")
            {
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color))
            };
            LogTextBlock.Inlines.Add(run);
            LogScrollViewer.ScrollToBottom();
        });
    }
    
    private void LogOnUI(string message, string color = "#E0E0E0")
    {
        Dispatcher.Invoke(() => Log(message, color));
    }
    
    private void SetStatus(string message, string color)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = message;
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
        });
    }
    
    private void SetStatusOnUI(string message, string color)
    {
        Dispatcher.Invoke(() => SetStatus(message, color));
    }
}
