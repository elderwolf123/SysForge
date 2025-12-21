using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using RamOptimizerNova.ViewModels;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer;
    private readonly SystemMetricsService _metricsService;
    private readonly WindowsCompactCompression _compressor;
    private readonly ProgramScanner _scanner;
    
    private string? _selectedPath;
    private InstalledProgram? _selectedProgram;
    
    // UI element references
    private TextBlock? _cpuText;
    private TextBlock? _gpuText;
    private TextBlock? _memoryText;
    private TextBlock? _storageText;
    private TextBlock? _cpuPercentText;
    private TextBlock? _gpuPercentText;
    private TextBlock? _memoryPercentText;
    private TextBlock? _storagePercentText;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel();
        
        _metricsService = new SystemMetricsService();
        _compressor = new WindowsCompactCompression();
        _scanner = new ProgramScanner();
        
        // Find UI elements
        _cpuText = this.FindControl<TextBlock>("CpuText");
        _gpuText = this.FindControl<TextBlock>("GpuText");
        _memoryText = this.FindControl<TextBlock>("MemoryText");
        _storageText = this.FindControl<TextBlock>("StorageText");
        _cpuPercentText = this.FindControl<TextBlock>("CpuPercentText");
        _gpuPercentText = this.FindControl<TextBlock>("GpuPercentText");
        _memoryPercentText = this.FindControl<TextBlock>("MemoryPercentText");
        _storagePercentText = this.FindControl<TextBlock>("StoragePercentText");
        
        // Setup metrics timer
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
        UpdateMetrics();
        
        // Wire up compression UI
        WireUpCompressionUI();
    }

    private void WireUpCompressionUI()
    {
        var fileModeRadio = this.FindControl<RadioButton>("FileModeRadio");
        var programModeRadio = this.FindControl<RadioButton>("ProgramModeRadio");
        var selectFolderButton = this.FindControl<Button>("SelectFolderButton");
        var scanProgramsButton = this.FindControl<Button>("ScanProgramsButton");
        var compressButton = this.FindControl<Button>("CompressButton");
        var decompressButton = this.FindControl<Button>("DecompressButton");
        
        if (fileModeRadio != null)
            fileModeRadio.Checked += (s, e) => SwitchMode(true);
        if (programModeRadio != null)
            programModeRadio.Checked += (s, e) => SwitchMode(false);
        if (selectFolderButton != null)
            selectFolderButton.Click += SelectFolder_Click;
        if (scanProgramsButton != null)
            scanProgramsButton.Click += ScanPrograms_Click;
        if (compressButton != null)
            compressButton.Click += Compress_Click;
        if (decompressButton != null)
            decompressButton.Click += Decompress_Click;
    }

    private void SwitchMode(bool fileMode)
    {
        var fileModePanel = this.FindControl<StackPanel>("FileModePanel");
        var programModePanel = this.FindControl<StackPanel>("ProgramModePanel");
        
        if (fileModePanel != null)
            fileModePanel.IsVisible = fileMode;
        if (programModePanel != null)
            programModePanel.IsVisible = !fileMode;
    }

    private async void SelectFolder_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Folder to Compress"
        };
        
        var result = await dialog.ShowAsync(this);
        
        if (!string.IsNullOrEmpty(result))
        {
            _selectedPath = result;
            var selectedPathText = this.FindControl<TextBlock>("SelectedPathText");
            if (selectedPathText != null)
            {
                var size = GetDirectorySize(result);
                selectedPathText.Text = $"Selected: {result}\nSize: {FormatBytes(size)}";
            }
        }
    }

    private async void ScanPrograms_Click(object? sender, RoutedEventArgs e)
    {
        var statusText = this.FindControl<TextBlock>("StatusText");
        var scanButton = this.FindControl<Button>("ScanProgramsButton");
        var programListBox = this.FindControl<ListBox>("ProgramListBox");
        
        if (statusText != null)
            statusText.Text = "Scanning for programs...";
        if (scanButton != null)
            scanButton.IsEnabled = false;
        
        try
        {
            var programs = await _scanner.ScanInstalledProgramsAsync();
            
            if (programListBox != null)
            {
                programListBox.Items.Clear();
                foreach (var program in programs.Take(50)) // Limit to top 50
                {
                    var item = new ListBoxItem
                    {
                        Content = $"{program.Name} - {program.SizeFormatted} ({program.Type}) {(program.IsCompressed ? "✓ Compressed" : "")}"
                    };
                    item.Tag = program;
                    programListBox.Items.Add(item);
                }
                
                programListBox.SelectionChanged += (s, e) =>
                {
                    if (programListBox.SelectedItem is ListBoxItem selected)
                    {
                        _selectedProgram = selected.Tag as InstalledProgram;
                        _selectedPath = _selectedProgram?.InstallPath;
                    }
                };
            }
            
            if (statusText != null)
                statusText.Text = $"Found {programs.Count} programs/games";
        }
        finally
        {
            if (scanButton != null)
                scanButton.IsEnabled = true;
        }
    }

    private async void Compress_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedPath)) return;
        
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
        
        try
        {
            var algorithm = algorithmCombo?.SelectedIndex switch
            {
                0 => CompactAlgorithm.XPRESS4K,
                1 => CompactAlgorithm.XPRESS8K,
                2 => CompactAlgorithm.XPRESS16K,
                _ => CompactAlgorithm.LZX
            };
            
            var result = await _compressor.CompressAsync(_selectedPath, algorithm);
            
            if (result.Success)
            {
                if (statusText != null)
                    statusText.Text = "✅ Compression complete!";
                if (resultsText != null)
                    resultsText.Text = $"Compressed {result.CompressedFiles} files\n" +
                                     $"Space saved: {FormatBytes(result.OriginalSize - result.CompressedSize)} ({result.SpaceSaved:P1})";
            }
            else
            {
                if (statusText != null)
                    statusText.Text = "❌ Compression failed";
                if (resultsText != null)
                    resultsText.Text = result.Error;
            }
        }
        finally
        {
            if (compressButton != null)
                compressButton.IsEnabled = true;
            if (progressBar != null)
                progressBar.Value = 100;
        }
    }

    private async void Decompress_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedPath)) return;
        
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
        
        try
        {
            var result = await _compressor.DecompressAsync(_selectedPath);
            
            if (result.Success)
            {
                if (statusText != null)
                    statusText.Text = "✅ Decompression complete!";
                if (resultsText != null)
                    resultsText.Text = $"Decompressed {result.TotalFiles} files";
            }
            else
            {
                if (statusText != null)
                    statusText.Text = "❌ Decompression failed";
                if (resultsText != null)
                    resultsText.Text = result.Error;
            }
        }
        finally
        {
            if (decompressButton != null)
                decompressButton.IsEnabled = true;
            if (progressBar != null)
                progressBar.Value = 100;
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateMetrics();
    }

    private void UpdateMetrics()
    {
        }
    }
}