using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.Views;

public partial class MainWindow : Window
{
    private readonly SystemMetricsService _metricsService;
    private readonly WindowsCompactCompression _compressor;
    private readonly ProgramScanner _scanner;
    private readonly DispatcherTimer _timer;
    
    private string? _selectedPath;
    private InstalledProgram? _selectedProgram;
    
    // UI element references
    private TextBlock? _cpuText;
    private TextBlock? _gpuText;
    private TextBlock? _memoryText;
    private TextBlock? _cpuPercentText;
    private TextBlock? _gpuPercentText;
    private TextBlock? _memoryPercentText;

    public MainWindow()
    {
        try
        {
            Console.WriteLine("=== MainWindow Initializing ===");
            InitializeComponent();
            
            Console.WriteLine("Creating services...");
            _metricsService = new SystemMetricsService();
            _compressor = new WindowsCompactCompression();
            _scanner = new ProgramScanner();
            
            Console.WriteLine("Finding UI elements...");
            FindUIElements();
            
            Console.WriteLine("Setting up metrics timer...");
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            
            Console.WriteLine("Initial metrics update...");
            UpdateMetrics();
            UpdateAllDrives();
            
            Console.WriteLine("Wiring compression UI...");
            WireCompressionUI();
            
            Console.WriteLine("=== MainWindow Initialized Successfully ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ CRITICAL ERROR in MainWindow constructor: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void FindUIElements()
    {
        try
        {
            _cpuText = this.FindControl<TextBlock>("CpuText");
            _gpuText = this.FindControl<TextBlock>("GpuText");
            _memoryText = this.FindControl<TextBlock>("MemoryText");
            _cpuPercentText = this.FindControl<TextBlock>("CpuPercentText");
            _gpuPercentText = this.FindControl<TextBlock>("GpuPercentText");
            _memoryPercentText = this.FindControl<TextBlock>("MemoryPercentText");
            
            Console.WriteLine($"✓ Found UI elements - CPU: {_cpuText != null}, GPU: {_gpuText != null}, Memory: {_memoryText != null}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error finding UI elements: {ex.Message}");
        }
    }

    private void WireCompressionUI()
    {
        try
        {
            Console.WriteLine("Wiring compression buttons...");
            
            var fileModeRadio = this.FindControl<RadioButton>("FileModeRadio");
            var programModeRadio = this.FindControl<RadioButton>("ProgramModeRadio");
            var selectFolderButton = this.FindControl<Button>("SelectFolderButton");
            var scanProgramsButton = this.FindControl<Button>("ScanProgramsButton");
            var compressButton = this.FindControl<Button>("CompressButton");
            var decompressButton = this.FindControl<Button>("DecompressButton");
            
            if (fileModeRadio != null)
            {
                fileModeRadio.Checked += (s, e) => SwitchMode(true);
                Console.WriteLine("✓ FileModeRadio wired");
            }
            
            if (programModeRadio != null)
            {
                programModeRadio.Checked += (s, e) => SwitchMode(false);
                Console.WriteLine("✓ ProgramModeRadio wired");
            }
            
            if (selectFolderButton != null)
            {
                selectFolderButton.Click += SelectFolder_Click;
                Console.WriteLine("✓ SelectFolderButton wired");
            }
            
            if (scanProgramsButton != null)
            {
                scanProgramsButton.Click += ScanPrograms_Click;
                Console.WriteLine("✓ ScanProgramsButton wired");
            }
            
            if (compressButton != null)
            {
                compressButton.Click += Compress_Click;
                Console.WriteLine("✓ CompressButton wired");
            }
            
            if (decompressButton != null)
            {
                decompressButton.Click += Decompress_Click;
                Console.WriteLine("✓ DecompressButton wired");
            }
            
            Console.WriteLine("=== All compression handlers wired successfully ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error wiring compression UI: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void SwitchMode(bool fileMode)
    {
        try
        {
            Console.WriteLine($"Switching mode to: {(fileMode ? "File" : "Program")}");
            
            var fileModePanel = this.FindControl<StackPanel>("FileModePanel");
            var programModePanel = this.FindControl<StackPanel>("ProgramModePanel");
            
            if (fileModePanel != null)
                fileModePanel.IsVisible = fileMode;
            if (programModePanel != null)
                programModePanel.IsVisible = !fileMode;
                
            Console.WriteLine($"✓ Mode switched successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error switching mode: {ex.Message}");
        }
    }

    private async void SelectFolder_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Console.WriteLine("=== SelectFolder_Click triggered ===");
            
            var dialog = new OpenFolderDialog
            {
                Title = "Select Folder to Compress"
            };
            
            Console.WriteLine("Opening folder dialog...");
            var result = await dialog.ShowAsync(this);
            
            if (!string.IsNullOrEmpty(result))
            {
                Console.WriteLine($"✓ Folder selected: {result}");
                _selectedPath = result;
                
                var selectedPathText = this.FindControl<TextBlock>("SelectedPathText");
                if (selectedPathText != null)
                {
                    Console.WriteLine("Calculating folder size...");
                    var size = GetDirectorySize(result);
                    Console.WriteLine($"✓ Folder size: {FormatBytes(size)}");
                    
                    selectedPathText.Text = $"Selected: {result}\nSize: {FormatBytes(size)}";
                }
            }
            else
            {
                Console.WriteLine("⚠ No folder selected (user cancelled)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR in SelectFolder_Click: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async void ScanPrograms_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Console.WriteLine("=== ScanPrograms_Click triggered ===");
            
            var statusText = this.FindControl<TextBlock>("StatusText");
            var scanButton = this.FindControl<Button>("ScanProgramsButton");
            var programListBox = this.FindControl<ListBox>("ProgramListBox");
            
            if (statusText != null)
                statusText.Text = "Scanning for programs...";
            if (scanButton != null)
                scanButton.IsEnabled = false;
            
            Console.WriteLine("Starting program scan...");
            var programs = await _scanner.ScanInstalledProgramsAsync();
            Console.WriteLine($"✓ Found {programs.Count} programs");
            
            if (programListBox != null)
            {
                programListBox.Items.Clear();
                var count = 0;
                foreach (var program in programs.Take(50))
                {
                    var item = new ListBoxItem
                    {
                        Content = $"{program.Name} - {program.SizeFormatted} ({program.Type}) {(program.IsCompressed ? "✓ Compressed" : "")}"
                    };
                    item.Tag = program;
                    programListBox.Items.Add(item);
                    count++;
                }
                Console.WriteLine($"✓ Added {count} programs to list");
                
                programListBox.SelectionChanged += (s, e) =>
                {
                    if (programListBox.SelectedItem is ListBoxItem selected)
                    {
                        _selectedProgram = selected.Tag as InstalledProgram;
                        _selectedPath = _selectedProgram?.InstallPath;
                        Console.WriteLine($"✓ Program selected: {_selectedProgram?.Name}");
                    }
                };
            }
            
            if (statusText != null)
                statusText.Text = $"Found {programs.Count} programs/games";
            if (scanButton != null)
                scanButton.IsEnabled = true;
                
            Console.WriteLine("=== Program scan complete ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR in ScanPrograms_Click: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText != null)
                statusText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            var scanButton = this.FindControl<Button>("ScanProgramsButton");
            if (scanButton != null)
                scanButton.IsEnabled = true;
        }
    }

    private async void Compress_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Console.WriteLine("=== Compress_Click triggered ===");
            
            if (string.IsNullOrEmpty(_selectedPath))
            {
                Console.WriteLine("⚠ No path selected");
                return;
            }
            
            Console.WriteLine($"Compressing: {_selectedPath}");
            
            var compressButton = this.FindControl<Button>("CompressButton");
            var statusText = this.FindControl<TextBlock>("StatusText");
            var progressBar = this.FindControl<ProgressBar>("CompressionProgress");
            var resultsText = this.FindControl<TextBlock>("ResultsText");
            var algorithmCombo = this.FindControl<ComboBox>("AlgorithmComboBox");
            
            if (compressButton != null)
                compressButton.IsEnabled = false;
            if (statusText != null)
                statusText.Text = "Compressing...";
            if (progressBar != null)
                progressBar.Value = 50;
            
            var algorithm = algorithmCombo?.SelectedIndex switch
            {
                0 => CompactAlgorithm.XPRESS4K,
                1 => CompactAlgorithm.XPRESS8K,
                2 => CompactAlgorithm.XPRESS16K,
                _ => CompactAlgorithm.LZX
            };
            
            Console.WriteLine($"Using algorithm: {algorithm}");
            Console.WriteLine("Starting compression...");
            
            var result = await _compressor.CompressAsync(_selectedPath, algorithm);
            
            Console.WriteLine($"Compression result - Success: {result.Success}");
            if (result.Success)
            {
                Console.WriteLine($"✓ Compressed {result.CompressedFiles} files");
                Console.WriteLine($"✓ Original: {FormatBytes(result.OriginalSize)}");
                Console.WriteLine($"✓ Compressed: {FormatBytes(result.CompressedSize)}");
                Console.WriteLine($"✓ Saved: {FormatBytes(result.OriginalSize - result.CompressedSize)} ({result.SpaceSaved:P1})");
                
                if (statusText != null)
                    statusText.Text = "✅ Compression complete!";
                if (resultsText != null)
                    resultsText.Text = $"Compressed {result.CompressedFiles} files\nSpace saved: {FormatBytes(result.OriginalSize - result.CompressedSize)} ({result.SpaceSaved:P1})";
            }
            else
            {
                Console.WriteLine($"❌ Compression failed: {result.Error}");
                if (statusText != null)
                    statusText.Text = "❌ Compression failed";
                if (resultsText != null)
                    resultsText.Text = result.Error;
            }
            
            if (progressBar != null)
                progressBar.Value = 100;
                
            Console.WriteLine("=== Compression complete ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR in Compress_Click: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            var statusText = this.FindControl<TextBlock>("StatusText");
            var resultsText = this.FindControl<TextBlock>("ResultsText");
            if (statusText != null)
                statusText.Text = "❌ Compression error";
            if (resultsText != null)
                resultsText.Text = ex.Message;
        }
        finally
        {
            var compressButton = this.FindControl<Button>("CompressButton");
            if (compressButton != null)
                compressButton.IsEnabled = true;
        }
    }

    private async void Decompress_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Console.WriteLine("=== Decompress_Click triggered ===");
            
            if (string.IsNullOrEmpty(_selectedPath))
            {
                Console.WriteLine("⚠ No path selected");
                return;
            }
            
            Console.WriteLine($"Decompressing: {_selectedPath}");
            
            var decompressButton = this.FindControl<Button>("DecompressButton");
            var statusText = this.FindControl<TextBlock>("StatusText");
            var progressBar = this.FindControl<ProgressBar>("CompressionProgress");
            var resultsText = this.FindControl<TextBlock>("ResultsText");
            
            if (decompressButton != null)
                decompressButton.IsEnabled = false;
            if (statusText != null)
                statusText.Text = "Decompressing...";
            if (progressBar != null)
                progressBar.Value = 50;
            
            Console.WriteLine("Starting decompression...");
            var result = await _compressor.DecompressAsync(_selectedPath);
            
            Console.WriteLine($"Decompression result - Success: {result.Success}");
            if (result.Success)
            {
                Console.WriteLine($"✓ Decompressed {result.TotalFiles} files");
                
                if (statusText != null)
                    statusText.Text = "✅ Decompression complete!";
                if (resultsText != null)
                    resultsText.Text = $"Decompressed {result.TotalFiles} files";
            }
            else
            {
                Console.WriteLine($"❌ Decompression failed: {result.Error}");
                if (statusText != null)
                    statusText.Text = "❌ Decompression failed";
                if (resultsText != null)
                    resultsText.Text = result.Error;
            }
            
            if (progressBar != null)
                progressBar.Value = 100;
                
            Console.WriteLine("=== Decompression complete ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR in Decompress_Click: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            var statusText = this.FindControl<TextBlock>("StatusText");
            var resultsText = this.FindControl<TextBlock>("ResultsText");
            if (statusText != null)
                statusText.Text = "❌ Decompression error";
            if (resultsText != null)
                resultsText.Text = ex.Message;
        }
        finally
        {
            var decompressButton = this.FindControl<Button>("DecompressButton");
            if (decompressButton != null)
                decompressButton.IsEnabled = true;
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateMetrics();
        UpdateAllDrives();
    }

    private void UpdateMetrics()
    {
        try
        {
            var cpu = _metricsService.GetCpuUsage();
            var memory = _metricsService.GetMemoryUsage();
            
            if (_cpuText != null) _cpuText.Text = cpu.ToString("F0");
            if (_cpuPercentText != null) _cpuPercentText.Text = $"{cpu:F0}% utilized";
            
            // GPU placeholder
            if (_gpuText != null) _gpuText.Text = "35";
            if (_gpuPercentText != null) _gpuPercentText.Text = "35% utilized";
            
            if (_memoryText != null) _memoryText.Text = memory.usedGB.ToString("F1");
            if (_memoryPercentText != null) _memoryPercentText.Text = $"{memory.percentage:F0}% utilized";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating metrics: {ex.Message}");
        }
    }

    private void UpdateAllDrives()
    {
        try
        {
            var drivesPanel = this.FindControl<ItemsControl>("DrivesPanel");
            if (drivesPanel == null) return;
            
            // For now, just log the drives - we'll implement UI in next iteration
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .ToList();
            
            Console.WriteLine($"Found {drives.Count} drives:");
            foreach (var d in drives)
            {
                Console.WriteLine($"  {d.Name} ({d.VolumeLabel}) - {FormatBytes(d.AvailableFreeSpace)} free of {FormatBytes(d.TotalSize)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating drives: {ex.Message}");
        }
    }

    private long GetDirectorySize(string path)
    {
        try
        {
            var dirInfo = new DirectoryInfo(path);
            return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(file => file.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Error getting directory size for {path}: {ex.Message}");
            return 0;
        }
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