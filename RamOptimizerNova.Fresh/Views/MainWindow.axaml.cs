using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
    private readonly CompressionAnalysisService _analysisService;
    private readonly DispatcherTimer _timer;
    private readonly FileLogger _logger = FileLogger.Instance;
    
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
            _logger.Log("=== MainWindow Initializing ===");
            InitializeComponent();
            
            _logger.Log("Creating services...");
            _metricsService = new SystemMetricsService();
            _compressor = new WindowsCompactCompression();
            _scanner = new ProgramScanner();
            _analysisService = new CompressionAnalysisService();
            
            _logger.Log("Finding UI elements...");
            FindUIElements();
            
            _logger.Log("Setting up metrics timer...");
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            
            _logger.Log("Initial metrics update...");
            UpdateMetrics();
            UpdateAllDrives();
            
            _logger.Log("Wiring compression UI...");
            WireCompressionUI();
            
            _logger.Log("=== MainWindow Initialized Successfully ===");
        }
        catch (Exception ex)
        {
            _logger.Log($"❌ CRITICAL ERROR in MainWindow constructor: {ex.Message}");
            _logger.Log($"Stack trace: {ex.StackTrace}");
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
            
            _logger.Log($"✓ Found UI elements - CPU: {_cpuText != null}, GPU: {_gpuText != null}, Memory: {_memoryText != null}");
        }
        catch (Exception ex)
        {
            _logger.Log($"❌ Error finding UI elements: {ex.Message}");
        }
    }

    private void WireCompressionUI()
    {
        try
        {
            _logger.Log("Wiring compression buttons...");
            
            var fileModeRadio = this.FindControl<RadioButton>("FileModeRadio");
            var programModeRadio = this.FindControl<RadioButton>("ProgramModeRadio");
            var selectFolderButton = this.FindControl<Button>("SelectFolderButton");
            var scanProgramsButton = this.FindControl<Button>("ScanProgramsButton");
            var estimateButton = this.FindControl<Button>("EstimateButton");
            var compressButton = this.FindControl<Button>("CompressButton");
            var decompressButton = this.FindControl<Button>("DecompressButton");
            
            if (fileModeRadio != null)
            {
                fileModeRadio.Checked += (s, e) => SwitchMode(true);
                _logger.Log("✓ FileModeRadio wired");
            }
            
            if (programModeRadio != null)
            {
                programModeRadio.Checked += (s, e) => SwitchMode(false);
                _logger.Log("✓ ProgramModeRadio wired");
            }
            
            if (selectFolderButton != null)
            {
                selectFolderButton.Click += SelectFolder_Click;
                _logger.Log("✓ SelectFolderButton wired");
            }
            
            if (scanProgramsButton != null)
            {
                scanProgramsButton.Click += ScanPrograms_Click;
                _logger.Log("✓ ScanProgramsButton wired");
            }
            
            if (estimateButton != null)
            {
                estimateButton.Click += Estimate_Click;
                _logger.Log("✓ EstimateButton wired");
            }
            
            if (compressButton != null)
            {
                compressButton.Click += Compress_Click;
                _logger.Log("✓ CompressButton wired");
            }
            
            if (decompressButton != null)
            {
                decompressButton.Click += Decompress_Click;
                _logger.Log("✓ DecompressButton wired");
            }
            
            _logger.Log("=== All compression handlers wired successfully ===");
        }
        catch (Exception ex)
        {
            _logger.Log($"❌ Error wiring compression UI: {ex.Message}");
            _logger.Log($"Stack trace: {ex.StackTrace}");
        }
    }

    private void SwitchMode(bool fileMode)
    {
        try
        {
            _logger.Log($"Switching mode to: {(fileMode ? "File" : "Program")}");
            
            var fileModePanel = this.FindControl<StackPanel>("FileModePanel");
            var programModePanel = this.FindControl<StackPanel>("ProgramModePanel");
            
            if (fileModePanel != null)
                fileModePanel.IsVisible = fileMode;
            if (programModePanel != null)
                programModePanel.IsVisible = !fileMode;
                
            _logger.Log($"✓ Mode switched successfully");
        }
        catch (Exception ex)
        {
            _logger.Log($"❌ Error switching mode: {ex.Message}");
        }
    }

    private async void SelectFolder_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Log("=== SelectFolder_Click triggered ===");
            
            var dialog = new OpenFolderDialog
            {
                Title = "Select Folder to Compress"
            };
            
            _logger.Log("Opening folder dialog...");
            var result = await dialog.ShowAsync(this);
            
            if (!string.IsNullOrEmpty(result))
            {
                _logger.Log($"✓ Folder selected: {result}");
                _selectedPath = result;
                
                var selectedPathText = this.FindControl<TextBlock>("SelectedPathText");
                if (selectedPathText != null)
                {
                    _logger.Log("Calculating folder size...");
                    var size = GetDirectorySize(result);
                    _logger.Log($"✓ Folder size: {FormatBytes(size)}");
                    
                    selectedPathText.Text = $"Selected: {result}\nSize: {FormatBytes(size)}";
                }
            }
            else
            {
                _logger.Log("⚠ No folder selected (user cancelled)");
            }
        }
        catch (Exception ex)
        {
            _logger.Log($"❌ ERROR in SelectFolder_Click: {ex.Message}");
            _logger.Log($"Stack trace: {ex.StackTrace}");
        }
    }

    private async void ScanPrograms_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Log("=== ScanPrograms_Click triggered ===");
            
            var statusText = this.FindControl<TextBlock>("StatusText");
            var scanButton = this.FindControl<Button>("ScanProgramsButton");
            var programListBox = this.FindControl<ListBox>("ProgramListBox");
            
            if (statusText != null)
                statusText.Text = "Scanning for programs...";
            if (scanButton != null)
                scanButton.IsEnabled = false;
            
            _logger.Log("Starting program scan...");
            var programs = await _scanner.ScanInstalledProgramsAsync();
            _logger.Log($"✓ Found {programs.Count} programs");
            
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
                _logger.Log($"✓ Added {count} programs to list");
                
                programListBox.SelectionChanged += (s, e) =>
                {
                    if (programListBox.SelectedItem is ListBoxItem selected)
                    {
                        _selectedProgram = selected.Tag as InstalledProgram;
                        _selectedPath = _selectedProgram?.InstallPath;
                        _logger.Log($"✓ Program selected: {_selectedProgram?.Name}");
                    }
                };
            }
            
            if (statusText != null)
                statusText.Text = $"Found {programs.Count} programs/games";
            if (scanButton != null)
                scanButton.IsEnabled = true;
                
            _logger.Log("=== Program scan complete ===");
        }
        catch (Exception ex)
        {
            _logger.Log($"❌ ERROR in ScanPrograms_Click: {ex.Message}");
            _logger.Log($"Stack trace: {ex.StackTrace}");
            
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
            _logger.Log("=== Compress_Click triggered ===");
            
            string? pathToCompress = _selectedPath ?? _selectedProgram?.InstallPath;
            
            if (string.IsNullOrEmpty(pathToCompress))
            {
                _logger.Log("⚠ No path selected");
                UpdateStatusText("❌ Please select a folder or program first");
                return;
            }
            
            _logger.Log($"Compressing: {pathToCompress}");
            
            var compressButton = this.FindControl<Button>("CompressButton");
            var algorithmCombo = this.FindControl<ComboBox>("AlgorithmComboBox");
            
            if (compressButton != null)
                compressButton.IsEnabled = false;
           
            // Reset progress
            UpdateProgress(0, "Starting compression...");
            UpdateStatusText("Compressing...");
            
            var algorithm = algorithmCombo?.SelectedIndex switch
            {
                0 => CompactAlgorithm.XPRESS4K,
                1 => CompactAlgorithm.XPRESS8K,
                2 => CompactAlgorithm.XPRESS16K,
                _ => CompactAlgorithm.LZX
            };
            
            _logger.Log($"Using algorithm: {algorithm}");
            _logger.Log("Starting compression...");
            
            // Create progress reporter with ETA calculation
            var startTime = DateTime.Now;
            var progress = new Progress<CompactProgress>(p =>
            {
                if (p.FilesProcessed > 0)
                {
                    // Calculate rough ETA
                    // We don't know total files ahead of time, so we'll estimate based on rate
                    var filesPerSecond = p.FilesProcessed / p.ElapsedSeconds;
                    var eta = filesPerSecond > 0 ? $"~{filesPerSecond:F0} dirs/sec" : "calculating...";
                    
                    // Update progress bar (can't calculate exact % without knowing total, so use activity indicator)
                    var percent = Math.Min(95, (int)(p.FilesProcessed / 10)); // Rough progress
                    
                    UpdateProgress(percent, $"Compressing: {p.FilesProcessed} directories processed... ({eta})");
                }
            });
            
            var result = await _compressor.CompressAsync(pathToCompress, algorithm, true, progress);
            
            // Set to 100% when complete
            UpdateProgress(100, "Compression complete!");
            
            _logger.Log($"Compression result - Success: {result.Success}");
            if (result.Success)
            {
                _logger.Log($"✓ Compressed {result.CompressedFiles} files");
                _logger.Log($"✓ Original: {FormatBytes(result.OriginalSize)}");
                _logger.Log($"✓ Compressed: {FormatBytes(result.CompressedSize)}");
                _logger.Log($"✓ Saved: {FormatBytes(result.OriginalSize - result.CompressedSize)} ({result.SpaceSaved:P1})");
                
                UpdateStatusText($"✅ Compressed {result.CompressedFiles:N0} files - Saved {FormatBytes(result.OriginalSize - result.CompressedSize)} ({result.SpaceSaved:P1})");
            }
            else
            {
                _logger.Log($"❌ Compression failed: {result.Error}");
                UpdateStatusText($"❌ Compression failed: {result.Error}");
            }
            
            _logger.Log("=== Compression complete ===");
            
            // Reset progress after delay
            await Task.Delay(3000);
            UpdateProgress(0, "Ready");
        }
        catch (Exception ex)
        {
            _logger.Log($"❌ ERROR in Compress_Click: {ex.Message}");
            _logger.Log($"Stack trace: {ex.StackTrace}");
            
            UpdateStatusText($"❌ Compression error: {ex.Message}");
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
            _logger.Log("=== Decompress_Click triggered ===");
            
            if (string.IsNullOrEmpty(_selectedPath))
            {
                _logger.Log("⚠ No path selected");
                return;
            }
            
            _logger.Log($"Decompressing: {_selectedPath}");
            
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
            
            _logger.Log("Starting decompression...");
            var result = await _compressor.DecompressAsync(_selectedPath);
            
            _logger.Log($"Decompression result - Success: {result.Success}");
            if (result.Success)
            {
                _logger.Log($"✓ Decompressed {result.TotalFiles} files");
                
                if (statusText != null)
                    statusText.Text = "✅ Decompression complete!";
                if (resultsText != null)
                    resultsText.Text = $"Decompressed {result.TotalFiles} files";
            }
            else
            {
                _logger.Log($"❌ Decompression failed: {result.Error}");
                if (statusText != null)
                    statusText.Text = "❌ Decompression failed";
                if (resultsText != null)
                    resultsText.Text = result.Error;
            }
            
            if (progressBar != null)
                progressBar.Value = 100;
                
            _logger.Log("=== Decompression complete ===");
        }
        catch (Exception ex)
        {
            _logger.Log($"❌ ERROR in Decompress_Click: {ex.Message}");
            _logger.Log($"Stack trace: {ex.StackTrace}");
            
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
            _logger.Log($"❌ Error updating metrics: {ex.Message}");
        }
    }

    private void UpdateAllDrives()
    {
        try
        {
            var drivesPanel = this.FindControl<ItemsControl>("DrivesPanel");
            if (drivesPanel == null || drivesPanel.Items == null) return;
            
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .ToList();
            
            _logger.Log($"Found {drives.Count} drives:");
            
            // Create drive data objects for binding
            var driveData = drives.Select(d => new
            {
                Name = $"{d.Name.TrimEnd('\\')} ({d.VolumeLabel})",
                FreeGB = (d.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0)).ToString("F0"),
                TotalText = $"of {FormatBytes(d.TotalSize)} total",
                PercentUsed = $"{(1 - (double)d.AvailableFreeSpace / d.TotalSize) * 100:F0}% used"
            }).ToList();
            
            // Populate UI - Clear and add items
            drivesPanel.Items.Clear();
            foreach (var drive in driveData)
            {
                drivesPanel.Items.Add(drive);
            }
            
            foreach (var d in drives)
            {
                _logger.Log($"  {d.Name} ({d.VolumeLabel}) - {FormatBytes(d.AvailableFreeSpace)} free of {FormatBytes(d.TotalSize)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating drives", ex);
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
            _logger.Log($"⚠ Error getting directory size for {path}: {ex.Message}");
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

    private async void Estimate_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            _logger.Log("=== Estimate_Click triggered ===");
            
            // Get the path to analyze
            string? pathToAnalyze = _selectedPath ?? _selectedProgram?.InstallPath;
            
            if (string.IsNullOrEmpty(pathToAnalyze))
            {
                _logger.LogWarning("No path selected for analysis");
                UpdateStatusText("❌ Please select a folder or program first");
                return;
            }

            _logger.Log($"Analyzing: {pathToAnalyze}");
            
            // Hide previous results
            var analysisResults = this.FindControl<Border>("AnalysisResults");
            if (analysisResults != null)
            {
                analysisResults.IsVisible = false;
            }
            
            // Reset progress
            UpdateProgress(0, "Starting analysis...");
            
            // Create progress reporter
            var progress = new Progress<AnalysisProgress>(p =>
            {
                if (p.FilesProcessed > 0)
                {
                    int percent = Math.Min(100, (int)(p.FilesProcessed / 10)); // Rough estimate
                    UpdateProgress(percent, $"Analyzing: {p.FilesProcessed} files... ({FormatBytes(p.EstimatedSavingsBytes)} estimated savings)");
                }
            });
            
            // Run analysis
            var result = await _analysisService.AnalyzeAsync(pathToAnalyze, progress);
            
            // Update progress to complete
            UpdateProgress(100, "Analysis complete!");
            
            // Display results
            DisplayAnalysisResults(result);
            
            _logger.Log($"✓ Analysis complete - {result.TotalFiles} files, {FormatBytes(result.EstimatedSavingsBytes)} estimated savings");
        }
        catch (Exception ex)
        {
            _logger.LogError("Estimate_Click failed", ex);
            UpdateStatusText($"❌ Analysis failed: {ex.Message}");
        }
        finally
        {
            // Reset progress after a delay
            await Task.Delay(2000);
            UpdateProgress(0, "Ready");
        }
    }

    private void DisplayAnalysisResults(AnalysisResult result)
    {
        try
        {
            var analysisResults = this.FindControl<Border>("AnalysisResults");
            var analysisSummary = this.FindControl<TextBlock>("AnalysisSummary");
            var analysisDetails = this.FindControl<TextBlock>("AnalysisDetails");
            
            if (analysisResults == null || analysisSummary == null || analysisDetails == null)
            {
                _logger.LogWarning("Analysis result controls not found");
                return;
            }
            
            // Calculate percentages
            double compressiblePercent = result.TotalBytes > 0 
                ? (double)result.CompressibleBytes / result.TotalBytes * 100 
                : 0;
            double savingsPercent = result.CompressibleBytes > 0 
                ? (double)result.EstimatedSavingsBytes / result.CompressibleBytes * 100 
                : 0;
            
            // Build summary
            analysisSummary.Text = $"Estimated Savings: {FormatBytes(result.EstimatedSavingsBytes)} ({savingsPercent:0.#}% of compressible files)";
            
            // Build details
            var detailsText = $"📁 Total: {result.TotalFiles:N0} files ({FormatBytes(result.TotalBytes)})\n";
            detailsText += $"✅ Compressible: {result.CompressibleFiles:N0} files ({FormatBytes(result.CompressibleBytes)}) - {compressiblePercent:0.#}%\n";
            detailsText += $"❌ Already Compressed: {result.TotalFiles - result.CompressibleFiles:N0} files\n";
            
            // Add top file types
            if (result.FileTypeBreakdown.Any())
            {
                var topTypes = result.FileTypeBreakdown.Values
                    .OrderByDescending(ft => ft.EstimatedSavingsBytes)
                    .Take(5)
                    .ToList();
                
                if (topTypes.Any())
                {
                    detailsText += $"\n📊 Top Compressible Types:\n";
                    foreach (var ft in topTypes)
                    {
                        if (ft.EstimatedSavingsBytes > 0)
                        {
                            detailsText += $"  • {ft.Extension}: {ft.FileCount:N0} files, est. {FormatBytes(ft.EstimatedSavingsBytes)} saved\n";
                        }
                    }
                }
            }
            
            analysisDetails.Text = detailsText;
            analysisResults.IsVisible = true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error displaying analysis results", ex);
        }
    }

    private void UpdateProgress(int percent, string message)
    {
        try
        {
            var progressBar = this.FindControl<ProgressBar>("ProgressBar");
            var progressText = this.FindControl<TextBlock>("ProgressText");
            
            if (progressBar != null)
            {
                progressBar.Value = percent;
            }
            
            if (progressText != null)
            {
                progressText.Text = message;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error updating progress: {ex.Message}");
        }
    }

    private void UpdateStatusText(string text)
    {
        try
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText != null)
            {
                statusText.Text = text;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error updating status: {ex.Message}");
        }
    }
}
