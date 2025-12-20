using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using RamOptimizerNova.Models;
using RamOptimizerNova.Services;
using RamOptimizerNova.ViewModels.Base;
using ReactiveUI;

namespace RamOptimizerNova.ViewModels.Pages;

/// <summary>
/// ViewModel for the Compression page
/// </summary>
public class CompressionViewModel : PageBaseViewModel, IThemeAwareViewModel, ISearchablePage
{
    private readonly IMetricsService _metricsService;
    private readonly ISystemService _systemService;
    private readonly IHardwareService _hardwareService;
    private readonly IOptimizationService _optimizationService;
    private readonly IDialogService _dialogService;
    private readonly ILoggerService _loggerService;
    private readonly INavigationService _navigationService;
    private readonly IPerformanceMonitoringService _performanceMonitoringService;
    private readonly ICompressionService _compressionService;

    private ObservableCollection<CompressionFileItem> _fileItems = new();
    private ObservableCollection<CompressionHistoryItem> _compressionHistory = new();
    private ObservableCollection<string> _compressionLevels = new();
    private ObservableCollection<string> _algorithms = new();
    private ObservableCollection<string> _compressionModes = new();

    private CompressionFileItem? _selectedFile;
    private string? _selectedCompressionLevel;
    private string? _selectedAlgorithm;
    private string? _selectedMode;
    private string _statusMessage = "Ready";
    private DateTime _lastUpdateTime = DateTime.Now;
    private CancellationTokenSource? _refreshCancellationTokenSource;

    // Compression settings
    private bool _deleteOriginalFiles = false;
    private bool _createBackups = true;
    private bool _verifyCompressedFiles = true;
    private bool _compressHiddenFiles = false;
    private bool _enableAutomaticCompression = false;
    private bool _integrateWithFileExplorer = false;
    private bool _enableNotifications = true;
    private bool _enableLogging = true;

    // Commands
    public RelayCommand RefreshCommand { get; }
    public RelayCommand QuickCompressCommand { get; }
    public RelayCommand BatchCompressCommand { get; }
    public RelayCommand SettingsCommand { get; }
    public RelayCommand AddFilesCommand { get; }
    public RelayCommand AddFolderCommand { get; }
    public RelayCommand ClearAllCommand { get; }
    public RelayCommand<CompressionFileItem> CompressFileCommand { get; }
    public RelayCommand<CompressionFileItem> RemoveFileCommand { get; }
    public RelayCommand<string> ApplyPresetCommand { get; }
    public RelayCommand ApplyAdvancedSettingsCommand { get; }
    public RelayCommand<string> NavigateToPageCommand { get; }

    // Properties
    public ObservableCollection<CompressionFileItem> FileItems
    {
        get => _fileItems;
        set => this.RaiseAndSetIfChanged(ref _fileItems, value);
    }

    public ObservableCollection<CompressionHistoryItem> CompressionHistory
    {
        get => _compressionHistory;
        set => this.RaiseAndSetIfChanged(ref _compressionHistory, value);
    }

    public ObservableCollection<string> CompressionLevels
    {
        get => _compressionLevels;
        set => this.RaiseAndSetIfChanged(ref _compressionLevels, value);
    }

    public ObservableCollection<string> Algorithms
    {
        get => _algorithms;
        set => this.RaiseAndSetIfChanged(ref _algorithms, value);
    }

    public ObservableCollection<string> CompressionModes
    {
        get => _compressionModes;
        set => this.RaiseAndSetIfChanged(ref _compressionModes, value);
    }

    public CompressionFileItem? SelectedFile
    {
        get => _selectedFile;
        set => this.RaiseAndSetIfChanged(ref _selectedFile, value);
    }

    public string? SelectedCompressionLevel
    {
        get => _selectedCompressionLevel;
        set => this.RaiseAndSetIfChanged(ref _selectedCompressionLevel, value);
    }

    public string? SelectedAlgorithm
    {
        get => _selectedAlgorithm;
        set => this.RaiseAndSetIfChanged(ref _selectedAlgorithm, value);
    }

    public string? SelectedMode
    {
        get => _selectedMode;
        set => this.RaiseAndSetIfChanged(ref _selectedMode, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public DateTime LastUpdateTime
    {
        get => _lastUpdateTime;
        set => this.RaiseAndSetIfChanged(ref _lastUpdateTime, value);
    }

    // Compression metrics
    public int TotalFiles => FileItems.Count;
    public double AverageCompressionRatio => FileItems.Any() ? FileItems.Average(f => f.CompressionRatio) : 0;
    public double AverageCompressionSpeed => FileItems.Any() ? FileItems.Average(f => f.CompressionSpeed) : 0;
    public string CompressionStats => GetCompressionStats();
    public double TotalSpaceSaved => FileItems.Sum(f => f.Size - f.CompressedSize);
    public double CurrentProgress => _compressionService?.CurrentProgress ?? 0;
    public double QueueProgress => _compressionService?.QueueProgress ?? 0;
    public string ProgressDetails => _compressionService?.ProgressDetails ?? "";
    public string EstimatedTimeRemaining => _compressionService?.EstimatedTimeRemaining ?? "";
    public object RecentCompressions => GetRecentCompressions();
    public object CompressionStatistics => GetCompressionStatistics();

    // Compression settings
    public bool DeleteOriginalFiles
    {
        get => _deleteOriginalFiles;
        set => this.RaiseAndSetIfChanged(ref _deleteOriginalFiles, value);
    }

    public bool CreateBackups
    {
        get => _createBackups;
        set => this.RaiseAndSetIfChanged(ref _createBackups, value);
    }

    public bool VerifyCompressedFiles
    {
        get => _verifyCompressedFiles;
        set => this.RaiseAndSetIfChanged(ref _verifyCompressedFiles, value);
    }

    public bool CompressHiddenFiles
    {
        get => _compressHiddenFiles;
        set => this.RaiseAndSetIfChanged(ref _compressHiddenFiles, value);
    }

    public bool EnableAutomaticCompression
    {
        get => _enableAutomaticCompression;
        set => this.RaiseAndSetIfChanged(ref _enableAutomaticCompression, value);
    }

    public bool IntegrateWithFileExplorer
    {
        get => _integrateWithFileExplorer;
        set => this.RaiseAndSetIfChanged(ref _integrateWithFileExplorer, value);
    }

    public bool EnableNotifications
    {
        get => _enableNotifications;
        set => this.RaiseAndSetIfChanged(ref _enableNotifications, value);
    }

    public bool EnableLogging
    {
        get => _enableLogging;
        set => this.RaiseAndSetIfChanged(ref _enableLogging, value);
    }

    public CompressionViewModel(
        IMetricsService metricsService,
        ISystemService systemService,
        IHardwareService hardwareService,
        IOptimizationService optimizationService,
        IDialogService dialogService,
        ILoggerService loggerService,
        INavigationService navigationService,
        IPerformanceMonitoringService performanceMonitoringService,
        ICompressionService compressionService) : base("Compression", navigationService)
    {
        _metricsService = metricsService;
        _systemService = systemService;
        _hardwareService = hardwareService;
        _optimizationService = optimizationService;
        _dialogService = dialogService;
        _loggerService = loggerService;
        _navigationService = navigationService;
        _performanceMonitoringService = performanceMonitoringService;
        _compressionService = compressionService;

        // Initialize commands
        RefreshCommand = new RelayCommand(RefreshCompressionData);
        QuickCompressCommand = new RelayCommand(QuickCompress);
        BatchCompressCommand = new RelayCommand(BatchCompress);
        SettingsCommand = new RelayCommand(OpenSettings);
        AddFilesCommand = new RelayCommand(AddFiles);
        AddFolderCommand = new RelayCommand(AddFolder);
        ClearAllCommand = new RelayCommand(ClearAll);
        CompressFileCommand = new RelayCommand<CompressionFileItem>(CompressFile);
        RemoveFileCommand = new RelayCommand<CompressionFileItem>(RemoveFile);
        ApplyPresetCommand = new RelayCommand<string>(ApplyPreset);
        ApplyAdvancedSettingsCommand = new RelayCommand(ApplyAdvancedSettings);
        NavigateToPageCommand = new RelayCommand<string>(NavigateToPage);

        // Subscribe to events
        _metricsService.MetricsUpdated += OnMetricsUpdated;
        _metricsService.MetricsError += OnMetricsError;
        _systemService.SystemInfoChanged += OnSystemInfoChanged;
        _systemService.SystemError += OnSystemError;
        _hardwareService.HardwareStatusChanged += OnHardwareStatusChanged;
        _hardwareService.HardwareError += OnHardwareError;
        _optimizationService.OptimizationStatusChanged += OnOptimizationStatusChanged;
        _optimizationService.OptimizationError += OnOptimizationError;
        _performanceMonitoringService.PerformanceMetricsUpdated += OnPerformanceMetricsUpdated;
        _performanceMonitoringService.PerformanceAlert += OnPerformanceAlert;
        _performanceMonitoringService.PerformanceError += OnPerformanceError;
        _compressionService.CompressionProgressChanged += OnCompressionProgressChanged;
        _compressionService.CompressionCompleted += OnCompressionCompleted;
        _compressionService.CompressionError += OnCompressionError;

        // Initialize compression
        InitializeCompression();
    }

    public override async Task InitializeAsync()
    {
        try
        {
            await ShowLoadingAsync("Initializing Compression...", async () =>
            {
                // Initialize performance monitoring
                await _performanceMonitoringService.InitializeAsync();
                await _performanceMonitoringService.StartMonitoringAsync();

                // Initialize compression service
                await _compressionService.InitializeAsync();

                // Get initial metrics
                CurrentMetrics = await _metricsService.GetCurrentMetricsAsync();

                // Initialize compression components
                InitializeFileItems();
                InitializeCompressionHistory();
                InitializeCompressionLevels();
                InitializeAlgorithms();
                InitializeCompressionModes();

                // Start real-time updates
                StartRealTimeUpdates();
            });
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error initializing compression: {ex.Message}");
            await _dialogService.ShowErrorAsync("Compression Error", $"Failed to initialize compression: {ex.Message}");
        }
    }

    public override async Task CleanupAsync()
    {
        try
        {
            // Stop real-time updates
            StopRealTimeUpdates();

            // Unsubscribe from events
            _metricsService.MetricsUpdated -= OnMetricsUpdated;
            _metricsService.MetricsError -= OnMetricsError;
            _systemService.SystemInfoChanged -= OnSystemInfoChanged;
            _systemService.SystemError -= OnSystemError;
            _hardwareService.HardwareStatusChanged -= OnHardwareStatusChanged;
            _hardwareService.HardwareError -= OnHardwareError;
            _optimizationService.OptimizationStatusChanged -= OnOptimizationStatusChanged;
            _optimizationService.OptimizationError -= OnOptimizationError;
            _performanceMonitoringService.PerformanceMetricsUpdated -= OnPerformanceMetricsUpdated;
            _performanceMonitoringService.PerformanceAlert -= OnPerformanceAlert;
            _performanceMonitoringService.PerformanceError -= OnPerformanceError;
            _compressionService.CompressionProgressChanged -= OnCompressionProgressChanged;
            _compressionService.CompressionCompleted -= OnCompressionCompleted;
            _compressionService.CompressionError -= OnCompressionError;
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error cleaning up compression: {ex.Message}");
        }
    }

    public override async Task RefreshAsync()
    {
        try
        {
            await RefreshCompressionData();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error refreshing compression: {ex.Message}");
            await _dialogService.ShowErrorAsync("Refresh Error", $"Failed to refresh compression: {ex.Message}");
        }
    }

    public async Task FocusSearchAsync()
    {
        try
        {
            // Focus search control in compression
            await _dialogService.ShowMessageAsync("Search", "Search functionality will be implemented in the next phase.");
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error focusing search: {ex.Message}");
        }
    }

    private void InitializeCompression()
    {
        try
        {
            // Initialize file items
            InitializeFileItems();

            // Initialize compression history
            InitializeCompressionHistory();

            // Initialize compression levels
            InitializeCompressionLevels();

            // Initialize algorithms
            InitializeAlgorithms();

            // Initialize compression modes
            InitializeCompressionModes();
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing compression: {ex.Message}").Wait();
        }
    }

    private void InitializeFileItems()
    {
        try
        {
            _fileItems.Clear();

            // Get recent files for compression
            var files = _systemService.GetRecentFiles(20).Result;
            
            foreach (var file in files)
            {
                _fileItems.Add(new CompressionFileItem
                {
                    Name = file.Name,
                    Path = file.Path,
                    Size = file.Size,
                    CompressedSize = file.Size, // Initially not compressed
                    CompressionRatio = 0,
                    Algorithm = "None",
                    Status = "Pending",
                    CompressionSpeed = 0,
                    ModifiedTime = file.ModifiedTime,
                    CreatedTime = file.CreatedTime,
                    IsHidden = file.IsHidden,
                    IsSystem = file.IsSystem
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing file items: {ex.Message}").Wait();
        }
    }

    private void InitializeCompressionHistory()
    {
        try
        {
            _compressionHistory.Clear();

            // Get compression history
            var history = _compressionService.GetCompressionHistoryAsync(30).Result;
            
            foreach (var item in history)
            {
                _compressionHistory.Add(new CompressionHistoryItem
                {
                    FileName = item.FileName,
                    OriginalSize = item.OriginalSize,
                    CompressedSize = item.CompressedSize,
                    CompressionRatio = item.CompressionRatio,
                    Algorithm = item.Algorithm,
                    CompressionTime = item.CompressionTime,
                    Status = item.Status
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing compression history: {ex.Message}").Wait();
        }
    }

    private void InitializeCompressionLevels()
    {
        try
        {
            _compressionLevels.Clear();

            // Add compression levels
            _compressionLevels.Add("None");
            _compressionLevels.Add("Fastest");
            _compressionLevels.Add("Fast");
            _compressionLevels.Add("Normal");
            _compressionLevels.Add("Maximum");
            _compressionLevels.Add("Ultra");

            // Set default
            SelectedCompressionLevel = "Normal";
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing compression levels: {ex.Message}").Wait();
        }
    }

    private void InitializeAlgorithms()
    {
        try
        {
            _algorithms.Clear();

            // Add compression algorithms
            _algorithms.Add("Deflate");
            _algorithms.Add("GZip");
            _algorithms.Add("BZip2");
            _algorithms.Add("LZMA");
            _algorithms.Add("LZ4");
            _algorithms.Add("Zstandard");
            _algorithms.Add("LZ77");

            // Set default
            SelectedAlgorithm = "LZMA";
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing algorithms: {ex.Message}").Wait();
        }
    }

    private void InitializeCompressionModes()
    {
        try
        {
            _compressionModes.Clear();

            // Add compression modes
            _compressionModes.Add("Single File");
            _compressionModes.Add("Batch");
            _compressionModes.Add("Recursive");
            _compressionModes.Add("Streaming");

            // Set default
            SelectedMode = "Single File";
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing compression modes: {ex.Message}").Wait();
        }
    }

    private void StartRealTimeUpdates()
    {
        _refreshCancellationTokenSource = new CancellationTokenSource();
        
        Task.Run(async () =>
        {
            while (!_refreshCancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await UpdateCompressionDataAsync();
                    await Task.Delay(3000, _refreshCancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await _loggerService.LogErrorAsync($"Error in compression real-time updates: {ex.Message}");
                    await Task.Delay(5000, _refreshCancellationTokenSource.Token); // Wait longer on error
                }
            }
        }, _refreshCancellationTokenSource.Token);
    }

    private void StopRealTimeUpdates()
    {
        _refreshCancellationTokenSource?.Cancel();
        _refreshCancellationTokenSource?.Dispose();
        _refreshCancellationTokenSource = null;
    }

    private async Task UpdateCompressionDataAsync()
    {
        try
        {
            // Update metrics
            CurrentMetrics = await _metricsService.GetCurrentMetricsAsync();

            // Update UI on main thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Update file items
                UpdateFileItems();

                // Update compression history
                UpdateCompressionHistory();

                // Update last update time
                LastUpdateTime = DateTime.Now;
            });
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error updating compression data: {ex.Message}");
        }
    }

    private void UpdateFileItems()
    {
        try
        {
            // Update file items with current data
            var files = _systemService.GetRecentFiles(20).Result;
            
            for (int i = 0; i < Math.Min(files.Count, _fileItems.Count); i++)
            {
                var file = files[i];
                var fileItem = _fileItems[i];
                
                fileItem.Name = file.Name;
                fileItem.Path = file.Path;
                fileItem.Size = file.Size;
                fileItem.ModifiedTime = file.ModifiedTime;
                fileItem.IsHidden = file.IsHidden;
                fileItem.IsSystem = file.IsSystem;
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating file items: {ex.Message}").Wait();
        }
    }

    private void UpdateCompressionHistory()
    {
        try
        {
            // Update compression history with new data
            var history = _compressionService.GetCompressionHistoryAsync(30).Result;
            
            _compressionHistory.Clear();
            
            foreach (var item in history)
            {
                _compressionHistory.Add(new CompressionHistoryItem
                {
                    FileName = item.FileName,
                    OriginalSize = item.OriginalSize,
                    CompressedSize = item.CompressedSize,
                    CompressionRatio = item.CompressionRatio,
                    Algorithm = item.Algorithm,
                    CompressionTime = item.CompressionTime,
                    Status = item.Status
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating compression history: {ex.Message}").Wait();
        }
    }

    private void RefreshCompressionData()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                StatusMessage = "Refreshing compression data...";
                
                await UpdateCompressionDataAsync();
                
                await _loggerService.LogAsync("Compression data refreshed successfully");
                StatusMessage = "Compression data refreshed successfully";
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error refreshing compression data: {ex.Message}");
                await _dialogService.ShowErrorAsync("Refresh Error", $"Failed to refresh compression data: {ex.Message}");
                StatusMessage = "Error refreshing compression data";
            }
        });
    }

    private async void QuickCompress()
    {
        try
        {
            StatusMessage = "Starting quick compression...";
            
            await _dialogService.ShowMessageAsync("Quick Compress", "Starting quick compression...");
            
            // Perform quick compression
            await _compressionService.QuickCompressAsync();
            
            await _dialogService.ShowMessageAsync("Quick Compress", "Quick compression completed successfully!");
            StatusMessage = "Quick compression completed successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error in quick compression: {ex.Message}");
            await _dialogService.ShowErrorAsync("Quick Compress Error", $"Failed to perform quick compression: {ex.Message}");
            StatusMessage = "Error in quick compression";
        }
    }

    private async void BatchCompress()
    {
        try
        {
            StatusMessage = "Starting batch compression...";
            
            await _dialogService.ShowMessageAsync("Batch Compress", "Starting batch compression...");
            
            // Start batch compression
            await _compressionService.StartBatchCompressionAsync();
            
            await _dialogService.ShowMessageAsync("Batch Compress", "Batch compression completed successfully!");
            StatusMessage = "Batch compression completed successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error in batch compression: {ex.Message}");
            await _dialogService.ShowErrorAsync("Batch Compress Error", $"Failed to perform batch compression: {ex.Message}");
            StatusMessage = "Error in batch compression";
        }
    }

    private async void OpenSettings()
    {
        try
        {
            StatusMessage = "Opening compression settings...";
            
            // Open compression settings dialog
            await _dialogService.ShowMessageAsync("Compression Settings", "Compression settings dialog will be implemented in the next phase.");
            
            StatusMessage = "Compression settings opened successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error opening compression settings: {ex.Message}");
            await _dialogService.ShowErrorAsync("Settings Error", $"Failed to open compression settings: {ex.Message}");
            StatusMessage = "Error opening compression settings";
        }
    }

    private async void AddFiles()
    {
        try
        {
            StatusMessage = "Adding files...";
            
            // Open file dialog to select files
            var result = await _dialogService.ShowFilePickerAsync("Select Files", true);
            
            if (result != null && result.Length > 0)
            {
                foreach (var file in result)
                {
                    _fileItems.Add(new CompressionFileItem
                    {
                        Name = System.IO.Path.GetFileName(file),
                        Path = file,
                        Size = new System.IO.FileInfo(file).Length,
                        CompressedSize = new System.IO.FileInfo(file).Length,
                        CompressionRatio = 0,
                        Algorithm = "None",
                        Status = "Pending",
                        CompressionSpeed = 0,
                        ModifiedTime = System.IO.File.GetLastWriteTime(file),
                        CreatedTime = System.IO.File.GetCreationTime(file),
                        IsHidden = (System.IO.File.GetAttributes(file) & System.IO.FileAttributes.Hidden) == System.IO.FileAttributes.Hidden,
                        IsSystem = (System.IO.File.GetAttributes(file) & System.IO.FileAttributes.System) == System.IO.FileAttributes.System
                    });
                }
                
                await _loggerService.LogAsync($"Added {result.Length} files to compression queue");
                StatusMessage = $"Added {result.Length} files successfully";
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error adding files: {ex.Message}");
            await _dialogService.ShowErrorAsync("Add Files Error", $"Failed to add files: {ex.Message}");
            StatusMessage = "Error adding files";
        }
    }

    private async void AddFolder()
    {
        try
        {
            StatusMessage = "Adding folder...";
            
            // Open folder dialog to select folder
            var result = await _dialogService.ShowFolderPickerAsync("Select Folder");
            
            if (!string.IsNullOrEmpty(result))
            {
                // Get all files in the folder
                var files = System.IO.Directory.GetFiles(result, "*.*", System.IO.SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    if (!CompressHiddenFiles && (System.IO.File.GetAttributes(file) & System.IO.FileAttributes.Hidden) == System.IO.FileAttributes.Hidden)
                        continue;
                    
                    _fileItems.Add(new CompressionFileItem
                    {
                        Name = System.IO.Path.GetFileName(file),
                        Path = file,
                        Size = new System.IO.FileInfo(file).Length,
                        CompressedSize = new System.IO.FileInfo(file).Length,
                        CompressionRatio = 0,
                        Algorithm = "None",
                        Status = "Pending",
                        CompressionSpeed = 0,
                        ModifiedTime = System.IO.File.GetLastWriteTime(file),
                        CreatedTime = System.IO.File.GetCreationTime(file),
                        IsHidden = (System.IO.File.GetAttributes(file) & System.IO.FileAttributes.Hidden) == System.IO.FileAttributes.Hidden,
                        IsSystem = (System.IO.File.GetAttributes(file) & System.IO.FileAttributes.System) == System.IO.FileAttributes.System
                    });
                }
                
                await _loggerService.LogAsync($"Added {files.Length} files from folder '{result}' to compression queue");
                StatusMessage = $"Added {files.Length} files successfully";
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error adding folder: {ex.Message}");
            await _dialogService.ShowErrorAsync("Add Folder Error", $"Failed to add folder: {ex.Message}");
            StatusMessage = "Error adding folder";
        }
    }

    private async void ClearAll()
    {
        try
        {
            var result = await _dialogService.ShowConfirmationAsync(
                "Clear All", 
                "Are you sure you want to clear all files from the compression queue?");
            
            if (result)
            {
                StatusMessage = "Clearing all files...";
                
                _fileItems.Clear();
                
                await _loggerService.LogAsync("Cleared all files from compression queue");
                StatusMessage = "All files cleared successfully";
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error clearing all files: {ex.Message}");
            await _dialogService.ShowErrorAsync("Clear All Error", $"Failed to clear all files: {ex.Message}");
            StatusMessage = "Error clearing all files";
        }
    }

    private async void CompressFile(CompressionFileItem file)
    {
        try
        {
            StatusMessage = $"Compressing file '{file.Name}'...";
            
            // Compress individual file
            await _compressionService.CompressFileAsync(file.Path, SelectedAlgorithm, SelectedCompressionLevel);
            
            // Update file status
            file.Status = "Compressed";
            file.Algorithm = SelectedAlgorithm;
            
            await _loggerService.LogAsync($"File '{file.Name}' compressed successfully");
            StatusMessage = "File compressed successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error compressing file: {ex.Message}");
            await _dialogService.ShowErrorAsync("Compress File Error", $"Failed to compress file: {ex.Message}");
            StatusMessage = "Error compressing file";
        }
    }

    private async void RemoveFile(CompressionFileItem file)
    {
        try
        {
            StatusMessage = $"Removing file '{file.Name}'...";
            
            // Remove file from queue
            _fileItems.Remove(file);
            
            await _loggerService.LogAsync($"File '{file.Name}' removed from compression queue");
            StatusMessage = "File removed successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error removing file: {ex.Message}");
            await _dialogService.ShowErrorAsync("Remove File Error", $"Failed to remove file: {ex.Message}");
            StatusMessage = "Error removing file";
        }
    }

    private async void ApplyPreset(string preset)
    {
        try
        {
            StatusMessage = $"Applying {preset} preset...";
            
            switch (preset.ToLower())
            {
                case "maximum":
                    await ApplyMaximumPresetAsync();
                    break;
                case "high":
                    await ApplyHighPresetAsync();
                    break;
                case "normal":
                    await ApplyNormalPresetAsync();
                    break;
                case "fast":
                    await ApplyFastPresetAsync();
                    break;
                case "ultrafast":
                    await ApplyUltraFastPresetAsync();
                    break;
                default:
                    await _dialogService.ShowMessageAsync("Preset Error", $"Unknown preset: {preset}");
                    break;
            }
            
            await _loggerService.LogAsync($"{preset} preset applied successfully");
            StatusMessage = $"{preset} preset applied successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error applying preset: {ex.Message}");
            await _dialogService.ShowErrorAsync("Preset Error", $"Failed to apply preset: {ex.Message}");
            StatusMessage = "Error applying preset";
        }
    }

    private async Task ApplyMaximumPresetAsync()
    {
        // Set maximum compression settings
        SelectedCompressionLevel = "Maximum";
        SelectedAlgorithm = "LZMA";
        SelectedMode = "Batch";
        
        // Enable all optimizations
        DeleteOriginalFiles = false;
        CreateBackups = true;
        VerifyCompressedFiles = true;
        CompressHiddenFiles = true;
        
        await _loggerService.LogAsync("Maximum compression preset applied");
    }

    private async Task ApplyHighPresetAsync()
    {
        // Set high compression settings
        SelectedCompressionLevel = "High";
        SelectedAlgorithm = "Deflate";
        SelectedMode = "Batch";
        
        // Enable most optimizations
        DeleteOriginalFiles = false;
        CreateBackups = true;
        VerifyCompressedFiles = true;
        CompressHiddenFiles = false;
        
        await _loggerService.LogAsync("High compression preset applied");
    }

    private async Task ApplyNormalPresetAsync()
    {
        // Set normal compression settings
        SelectedCompressionLevel = "Normal";
        SelectedAlgorithm = "GZip";
        SelectedMode = "Single File";
        
        // Enable balanced optimizations
        DeleteOriginalFiles = false;
        CreateBackups = true;
        VerifyCompressedFiles = true;
        CompressHiddenFiles = false;
        
        await _loggerService.LogAsync("Normal compression preset applied");
    }

    private async Task ApplyFastPresetAsync()
    {
        // Set fast compression settings
        SelectedCompressionLevel = "Fast";
        SelectedAlgorithm = "LZ4";
        SelectedMode = "Single File";
        
        // Enable minimal optimizations
        DeleteOriginalFiles = false;
        CreateBackups = false;
        VerifyCompressedFiles = false;
        CompressHiddenFiles = false;
        
        await _loggerService.LogAsync("Fast compression preset applied");
    }

    private async Task ApplyUltraFastPresetAsync()
    {
        // Set ultra fast compression settings
        SelectedCompressionLevel = "Fastest";
        SelectedAlgorithm = "LZ77";
        SelectedMode = "Streaming";
        
        // Disable most optimizations
        DeleteOriginalFiles = false;
        CreateBackups = false;
        VerifyCompressedFiles = false;
        CompressHiddenFiles = false;
        
        await _loggerService.LogAsync("Ultra fast compression preset applied");
    }

    private async void ApplyAdvancedSettings()
    {
        try
        {
            StatusMessage = "Applying advanced settings...";
            
            // Apply advanced compression settings
            await _compressionService.SetCompressionSettingsAsync(
                DeleteOriginalFiles,
                CreateBackups,
                VerifyCompressedFiles,
                CompressHiddenFiles,
                EnableAutomaticCompression,
                IntegrateWithFileExplorer,
                EnableNotifications,
                EnableLogging);
            
            await _loggerService.LogAsync("Advanced settings applied successfully");
            StatusMessage = "Advanced settings applied successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error applying advanced settings: {ex.Message}");
            await _dialogService.ShowErrorAsync("Advanced Settings Error", $"Failed to apply advanced settings: {ex.Message}");
            StatusMessage = "Error applying advanced settings";
        }
    }

    private async void NavigateToPage(string pageName)
    {
        try
        {
            await NavigateToPageAsync(pageName);
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error navigating to page: {ex.Message}");
        }
    }

    private string GetCompressionStats()
    {
        try
        {
            var totalFiles = FileItems.Count;
            var compressedFiles = FileItems.Count(f => f.Status == "Compressed");
            var totalSize = FileItems.Sum(f => f.Size);
            var compressedSize = FileItems.Sum(f => f.CompressedSize);
            var spaceSaved = totalSize - compressedSize;
            
            return $"Total: {totalFiles} files, {compressedFiles} compressed, {spaceSaved / 1024 / 1024:F1} MB saved";
        }
        catch
        {
            return "No compression data available";
        }
    }

    private object GetRecentCompressions()
    {
        try
        {
            // Return recent compressions as a list
            return CompressionHistory.Take(10).ToList();
        }
        catch
        {
            return new List<CompressionHistoryItem>();
        }
    }

    private object GetCompressionStatistics()
    {
        try
        {
            // Return compression statistics
            return new
            {
                TotalCompressions = CompressionHistory.Count,
                AverageRatio = CompressionHistory.Any() ? CompressionHistory.Average(c => c.CompressionRatio) : 0,
                TotalSpaceSaved = CompressionHistory.Sum(c => c.OriginalSize - c.CompressedSize),
                MostUsedAlgorithm = CompressionHistory.GroupBy(c => c.Algorithm)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?
                    .Key ?? "None"
            };
        }
        catch
        {
            return new { TotalCompressions = 0, AverageRatio = 0, TotalSpaceSaved = 0, MostUsedAlgorithm = "None" };
        }
    }

    // Event Handlers
    private void OnMetricsUpdated(object? sender, MetricsUpdatedEventArgs e)
    {
        _ = UpdateCompressionDataAsync();
    }

    private void OnMetricsError(object? sender, MetricsErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.ShowErrorAsync("Metrics Error", e.ErrorMessage);
        });
    }

    private void OnSystemInfoChanged(object? sender, SystemInfoChangedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Update system info related properties
            OnPropertyChanged(nameof(CompressionStats));
        });
    }

    private void OnSystemError(object? sender, SystemErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.ShowErrorAsync("System Error", e.ErrorMessage);
        });
    }

    private void OnHardwareStatusChanged(object? sender, HardwareStatusChangedEventArgs e)
    {
        _ = UpdateCompressionDataAsync();
    }

    private void OnHardwareError(object? sender, HardwareErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.ShowErrorAsync("Hardware Error", e.ErrorMessage);
        });
    }

    private void OnOptimizationStatusChanged(object? sender, OptimizationStatusChangedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusMessage = e.Status;
        });
    }

    private void OnOptimizationError(object? sender, OptimizationErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.ShowErrorAsync("Optimization Error", e.ErrorMessage);
        });
    }

    private void OnPerformanceMetricsUpdated(object? sender, PerformanceMetricsUpdatedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // Update current metrics with performance data
            CurrentMetrics = e.Metrics;
            await UpdateCompressionDataAsync();
        });
    }

    private void OnPerformanceAlert(object? sender, PerformanceAlertEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _loggerService.LogWarningAsync($"Performance alert: {e.Alert.Message}");
            StatusMessage = $"Performance alert: {e.Alert.Message}";
        });
    }

    private void OnPerformanceError(object? sender, PerformanceErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.ShowErrorAsync("Performance Error", e.Message);
            await _loggerService.LogErrorAsync($"Performance error: {e.Message}");
        });
    }

    private void OnCompressionProgressChanged(object? sender, CompressionProgressEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Update compression progress
            OnPropertyChanged(nameof(CurrentProgress));
            OnPropertyChanged(nameof(QueueProgress));
            OnPropertyChanged(nameof(ProgressDetails));
            OnPropertyChanged(nameof(EstimatedTimeRemaining));
        });
    }

    private void OnCompressionCompleted(object? sender, CompressionCompletedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _loggerService.LogAsync($"Compression completed: {e.FileName}");
            StatusMessage = $"Compression completed: {e.FileName}";
            
            // Update file status
            var file = FileItems.FirstOrDefault(f => f.Name == e.FileName);
            if (file != null)
            {
                file.Status = "Compressed";
                file.CompressionRatio = e.CompressionRatio;
                file.CompressedSize = e.CompressedSize;
                file.Algorithm = e.Algorithm;
            }
        });
    }

    private void OnCompressionError(object? sender, CompressionErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _loggerService.LogErrorAsync($"Compression error: {e.Message}");
            await _dialogService.ShowErrorAsync("Compression Error", e.Message);
            StatusMessage = $"Compression error: {e.Message}";
        });
    }

    public async Task OnThemeChangedAsync(ThemeVariant newTheme)
    {
        try
        {
            await _loggerService.LogAsync($"Compression theme changed to {newTheme}");
            await UpdateCompressionDataAsync();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error handling theme change: {ex.Message}");
        }
    }
}

// Supporting classes
public class CompressionFileItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public long Size { get; set; }
    public long CompressedSize { get; set; }
    public double CompressionRatio { get; set; }
    public string Algorithm { get; set; } = "";
    public string Status { get; set; } = "";
    public double CompressionSpeed { get; set; }
    public DateTime ModifiedTime { get; set; }
    public DateTime CreatedTime { get; set; }
    public bool IsHidden { get; set; }
    public bool IsSystem { get; set; }
}

public class CompressionHistoryItem
{
    public string FileName { get; set; } = "";
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public double CompressionRatio { get; set; }
    public string Algorithm { get; set; } = "";
    public DateTime CompressionTime { get; set; }
    public string Status { get; set; } = "";
}