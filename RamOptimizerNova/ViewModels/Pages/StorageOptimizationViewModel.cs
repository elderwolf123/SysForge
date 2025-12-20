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
/// ViewModel for the Storage Optimization page
/// </summary>
public class StorageOptimizationViewModel : PageBaseViewModel, IThemeAwareViewModel, ISearchablePage
{
    private readonly IMetricsService _metricsService;
    private readonly ISystemService _systemService;
    private readonly IHardwareService _hardwareService;
    private readonly IOptimizationService _optimizationService;
    private readonly IDialogService _dialogService;
    private readonly ILoggerService _loggerService;
    private readonly INavigationService _navigationService;
    private readonly IPerformanceMonitoringService _performanceMonitoringService;
    private readonly IStorageService _storageService;

    private ObservableCollection<StorageFileItem> _fileItems = new();
    private ObservableCollection<StorageHistoryItem> _storageHistory = new();
    private ObservableCollection<string> _cleanLevels = new();
    private ObservableCollection<string> _cleanModes = new();

    private StorageFileItem? _selectedFile;
    private string? _selectedCleanLevel;
    private string? _selectedCleanMode;
    private string _statusMessage = "Ready";
    private DateTime _lastUpdateTime = DateTime.Now;
    private CancellationTokenSource? _refreshCancellationTokenSource;

    // Storage settings
    private bool _cleanTemporaryFiles = true;
    private bool _cleanBrowserCache = true;
    private bool _cleanDownloadHistory = true;
    private bool _cleanRecycleBin = true;
    private bool _cleanSystemLogs = false;
    private bool _cleanApplicationData = false;
    private bool _enableAutomaticCleaning = false;
    private bool _cleanOnLowDiskSpace = true;
    private bool _cleanScheduledTasks = false;
    private bool _cleanSystemRestorePoints = false;
    private bool _integrateWithFileExplorer = false;
    private bool _enableNotifications = true;
    private bool _enableLogging = true;
    private bool _enableBackup = true;

    // Commands
    public RelayCommand RefreshCommand { get; }
    public RelayCommand QuickCleanCommand { get; }
    public RelayCommand DeepCleanCommand { get; }
    public RelayCommand SettingsCommand { get; }
    public RelayCommand AddFilesCommand { get; }
    public RelayCommand AddFolderCommand { get; }
    public RelayCommand ClearAllCommand { get; }
    public RelayCommand<StorageFileItem> CleanFileCommand { get; }
    public RelayCommand<StorageFileItem> RemoveFileCommand { get; }
    public RelayCommand<string> ApplyPresetCommand { get; }
    public RelayCommand ApplyAdvancedSettingsCommand { get; }
    public RelayCommand<string> NavigateToPageCommand { get; }

    // Properties
    public ObservableCollection<StorageFileItem> FileItems
    {
        get => _fileItems;
        set => this.RaiseAndSetIfChanged(ref _fileItems, value);
    }

    public ObservableCollection<StorageHistoryItem> StorageHistory
    {
        get => _storageHistory;
        set => this.RaiseAndSetIfChanged(ref _storageHistory, value);
    }

    public ObservableCollection<string> CleanLevels
    {
        get => _cleanLevels;
        set => this.RaiseAndSetIfChanged(ref _cleanLevels, value);
    }

    public ObservableCollection<string> CleanModes
    {
        get => _cleanModes;
        set => this.RaiseAndSetIfChanged(ref _cleanModes, value);
    }

    public StorageFileItem? SelectedFile
    {
        get => _selectedFile;
        set => this.RaiseAndSetIfChanged(ref _selectedFile, value);
    }

    public string? SelectedCleanLevel
    {
        get => _selectedCleanLevel;
        set => this.RaiseAndSetIfChanged(ref _selectedCleanLevel, value);
    }

    public string? SelectedCleanMode
    {
        get => _selectedCleanMode;
        set => this.RaiseAndSetIfChanged(ref _selectedCleanMode, value);
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

    // Storage metrics
    public long TotalStorage => _storageService?.TotalStorage ?? 0;
    public long UsedStorage => _storageService?.UsedStorage ?? 0;
    public long FreeStorage => _storageService?.FreeStorage ?? 0;
    public double UsedStoragePercentage => TotalStorage > 0 ? (double)UsedStorage / TotalStorage * 100 : 0;
    public string StorageStats => GetStorageStats();
    public int FilesCleaned => _storageService?.FilesCleaned ?? 0;
    public double SpaceReclaimed => _storageService?.SpaceReclaimed ?? 0;
    public double CurrentProgress => _storageService?.CurrentProgress ?? 0;
    public double QueueProgress => _storageService?.QueueProgress ?? 0;
    public string ProgressDetails => _storageService?.ProgressDetails ?? "";
    public string EstimatedTimeRemaining => _storageService?.EstimatedTimeRemaining ?? "";
    public object FileTypeDistribution => GetFileTypeDistribution();
    public object LargeFiles => GetLargeFiles();
    public object DuplicateFiles => GetDuplicateFiles();
    public object TemporaryFiles => GetTemporaryFiles();
    public object RecentCleans => GetRecentCleans();
    public object StorageStatistics => GetStorageStatistics();

    // Storage settings
    public bool CleanTemporaryFiles
    {
        get => _cleanTemporaryFiles;
        set => this.RaiseAndSetIfChanged(ref _cleanTemporaryFiles, value);
    }

    public bool CleanBrowserCache
    {
        get => _cleanBrowserCache;
        set => this.RaiseAndSetIfChanged(ref _cleanBrowserCache, value);
    }

    public bool CleanDownloadHistory
    {
        get => _cleanDownloadHistory;
        set => this.RaiseAndSetIfChanged(ref _cleanDownloadHistory, value);
    }

    public bool CleanRecycleBin
    {
        get => _cleanRecycleBin;
        set => this.RaiseAndSetIfChanged(ref _cleanRecycleBin, value);
    }

    public bool CleanSystemLogs
    {
        get => _cleanSystemLogs;
        set => this.RaiseAndSetIfChanged(ref _cleanSystemLogs, value);
    }

    public bool CleanApplicationData
    {
        get => _cleanApplicationData;
        set => this.RaiseAndSetIfChanged(ref _cleanApplicationData, value);
    }

    public bool EnableAutomaticCleaning
    {
        get => _enableAutomaticCleaning;
        set => this.RaiseAndSetIfChanged(ref _enableAutomaticCleaning, value);
    }

    public bool CleanOnLowDiskSpace
    {
        get => _cleanOnLowDiskSpace;
        set => this.RaiseAndSetIfChanged(ref _cleanOnLowDiskSpace, value);
    }

    public bool CleanScheduledTasks
    {
        get => _cleanScheduledTasks;
        set => this.RaiseAndSetIfChanged(ref _cleanScheduledTasks, value);
    }

    public bool CleanSystemRestorePoints
    {
        get => _cleanSystemRestorePoints;
        set => this.RaiseAndSetIfChanged(ref _cleanSystemRestorePoints, value);
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

    public bool EnableBackup
    {
        get => _enableBackup;
        set => this.RaiseAndSetIfChanged(ref _enableBackup, value);
    }

    public StorageOptimizationViewModel(
        IMetricsService metricsService,
        ISystemService systemService,
        IHardwareService hardwareService,
        IOptimizationService optimizationService,
        IDialogService dialogService,
        ILoggerService loggerService,
        INavigationService navigationService,
        IPerformanceMonitoringService performanceMonitoringService,
        IStorageService storageService) : base("Storage", navigationService)
    {
        _metricsService = metricsService;
        _systemService = systemService;
        _hardwareService = hardwareService;
        _optimizationService = optimizationService;
        _dialogService = dialogService;
        _loggerService = loggerService;
        _navigationService = navigationService;
        _performanceMonitoringService = performanceMonitoringService;
        _storageService = storageService;

        // Initialize commands
        RefreshCommand = new RelayCommand(RefreshStorageData);
        QuickCleanCommand = new RelayCommand(QuickClean);
        DeepCleanCommand = new RelayCommand(DeepClean);
        SettingsCommand = new RelayCommand(OpenSettings);
        AddFilesCommand = new RelayCommand(AddFiles);
        AddFolderCommand = new RelayCommand(AddFolder);
        ClearAllCommand = new RelayCommand(ClearAll);
        CleanFileCommand = new RelayCommand<StorageFileItem>(CleanFile);
        RemoveFileCommand = new RelayCommand<StorageFileItem>(RemoveFile);
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
        _storageService.StorageProgressChanged += OnStorageProgressChanged;
        _storageService.StorageCompleted += OnStorageCompleted;
        _storageService.StorageError += OnStorageError;

        // Initialize storage
        InitializeStorage();
    }

    public override async Task InitializeAsync()
    {
        try
        {
            await ShowLoadingAsync("Initializing Storage...", async () =>
            {
                // Initialize performance monitoring
                await _performanceMonitoringService.InitializeAsync();
                await _performanceMonitoringService.StartMonitoringAsync();

                // Initialize storage service
                await _storageService.InitializeAsync();

                // Get initial metrics
                CurrentMetrics = await _metricsService.GetCurrentMetricsAsync();

                // Initialize storage components
                InitializeFileItems();
                InitializeStorageHistory();
                InitializeCleanLevels();
                InitializeCleanModes();

                // Start real-time updates
                StartRealTimeUpdates();
            });
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error initializing storage: {ex.Message}");
            await _dialogService.ShowErrorAsync("Storage Error", $"Failed to initialize storage: {ex.Message}");
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
            _storageService.StorageProgressChanged -= OnStorageProgressChanged;
            _storageService.StorageCompleted -= OnStorageCompleted;
            _storageService.StorageError -= OnStorageError;
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error cleaning up storage: {ex.Message}");
        }
    }

    public override async Task RefreshAsync()
    {
        try
        {
            await RefreshStorageData();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error refreshing storage: {ex.Message}");
            await _dialogService.ShowErrorAsync("Refresh Error", $"Failed to refresh storage: {ex.Message}");
        }
    }

    public async Task FocusSearchAsync()
    {
        try
        {
            // Focus search control in storage
            await _dialogService.ShowMessageAsync("Search", "Search functionality will be implemented in the next phase.");
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error focusing search: {ex.Message}");
        }
    }

    private void InitializeStorage()
    {
        try
        {
            // Initialize file items
            InitializeFileItems();

            // Initialize storage history
            InitializeStorageHistory();

            // Initialize clean levels
            InitializeCleanLevels();

            // Initialize clean modes
            InitializeCleanModes();
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing storage: {ex.Message}").Wait();
        }
    }

    private void InitializeFileItems()
    {
        try
        {
            _fileItems.Clear();

            // Get files for cleaning
            var files = _storageService.GetFilesForCleaning(50).Result;
            
            foreach (var file in files)
            {
                _fileItems.Add(new StorageFileItem
                {
                    Name = file.Name,
                    Path = file.Path,
                    Size = file.Size,
                    Type = file.Type,
                    LastModified = file.LastModified,
                    Status = file.Status,
                    IsSelected = false,
                    IsSystem = file.IsSystem,
                    IsTemporary = file.IsTemporary,
                    IsDuplicate = file.IsDuplicate
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing file items: {ex.Message}").Wait();
        }
    }

    private void InitializeStorageHistory()
    {
        try
        {
            _storageHistory.Clear();

            // Get storage history
            var history = _storageService.GetStorageHistoryAsync(30).Result;
            
            foreach (var item in history)
            {
                _storageHistory.Add(new StorageHistoryItem
                {
                    CleanDate = item.CleanDate,
                    FilesCleaned = item.FilesCleaned,
                    SpaceReclaimed = item.SpaceReclaimed,
                    CleanLevel = item.CleanLevel,
                    Status = item.Status
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing storage history: {ex.Message}").Wait();
        }
    }

    private void InitializeCleanLevels()
    {
        try
        {
            _cleanLevels.Clear();

            // Add clean levels
            _cleanLevels.Add("Quick");
            _cleanLevels.Add("Standard");
            _cleanLevels.Add("Aggressive");
            _cleanLevels.Add("Maximum");

            // Set default
            SelectedCleanLevel = "Standard";
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing clean levels: {ex.Message}").Wait();
        }
    }

    private void InitializeCleanModes()
    {
        try
        {
            _cleanModes.Clear();

            // Add clean modes
            _cleanModes.Add("Single File");
            _cleanModes.Add("Batch");
            _cleanModes.Add("Recursive");
            _cleanModes.Add("Scheduled");

            // Set default
            SelectedCleanMode = "Batch";
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing clean modes: {ex.Message}").Wait();
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
                    await UpdateStorageDataAsync();
                    await Task.Delay(3000, _refreshCancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await _loggerService.LogErrorAsync($"Error in storage real-time updates: {ex.Message}");
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

    private async Task UpdateStorageDataAsync()
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

                // Update storage history
                UpdateStorageHistory();

                // Update last update time
                LastUpdateTime = DateTime.Now;
            });
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error updating storage data: {ex.Message}");
        }
    }

    private void UpdateFileItems()
    {
        try
        {
            // Update file items with current data
            var files = _storageService.GetFilesForCleaning(50).Result;
            
            for (int i = 0; i < Math.Min(files.Count, _fileItems.Count); i++)
            {
                var file = files[i];
                var fileItem = _fileItems[i];
                
                fileItem.Name = file.Name;
                fileItem.Path = file.Path;
                fileItem.Size = file.Size;
                fileItem.Type = file.Type;
                fileItem.LastModified = file.LastModified;
                fileItem.Status = file.Status;
                fileItem.IsSystem = file.IsSystem;
                fileItem.IsTemporary = file.IsTemporary;
                fileItem.IsDuplicate = file.IsDuplicate;
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating file items: {ex.Message}").Wait();
        }
    }

    private void UpdateStorageHistory()
    {
        try
        {
            // Update storage history with new data
            var history = _storageService.GetStorageHistoryAsync(30).Result;
            
            _storageHistory.Clear();
            
            foreach (var item in history)
            {
                _storageHistory.Add(new StorageHistoryItem
                {
                    CleanDate = item.CleanDate,
                    FilesCleaned = item.FilesCleaned,
                    SpaceReclaimed = item.SpaceReclaimed,
                    CleanLevel = item.CleanLevel,
                    Status = item.Status
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating storage history: {ex.Message}").Wait();
        }
    }

    private void RefreshStorageData()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                StatusMessage = "Refreshing storage data...";
                
                await UpdateStorageDataAsync();
                
                await _loggerService.LogAsync("Storage data refreshed successfully");
                StatusMessage = "Storage data refreshed successfully";
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error refreshing storage data: {ex.Message}");
                await _dialogService.ShowErrorAsync("Refresh Error", $"Failed to refresh storage data: {ex.Message}");
                StatusMessage = "Error refreshing storage data";
            }
        });
    }

    private async void QuickClean()
    {
        try
        {
            StatusMessage = "Starting quick clean...";
            
            await _dialogService.ShowMessageAsync("Quick Clean", "Starting quick clean...");
            
            // Perform quick clean
            await _storageService.QuickCleanAsync();
            
            await _dialogService.ShowMessageAsync("Quick Clean", "Quick clean completed successfully!");
            StatusMessage = "Quick clean completed successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error in quick clean: {ex.Message}");
            await _dialogService.ShowErrorAsync("Quick Clean Error", $"Failed to perform quick clean: {ex.Message}");
            StatusMessage = "Error in quick clean";
        }
    }

    private async void DeepClean()
    {
        try
        {
            StatusMessage = "Starting deep clean...";
            
            await _dialogService.ShowMessageAsync("Deep Clean", "Starting deep clean...");
            
            // Start deep clean
            await _storageService.StartDeepCleanAsync();
            
            await _dialogService.ShowMessageAsync("Deep Clean", "Deep clean completed successfully!");
            StatusMessage = "Deep clean completed successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error in deep clean: {ex.Message}");
            await _dialogService.ShowErrorAsync("Deep Clean Error", $"Failed to perform deep clean: {ex.Message}");
            StatusMessage = "Error in deep clean";
        }
    }

    private async void OpenSettings()
    {
        try
        {
            StatusMessage = "Opening storage settings...";
            
            // Open storage settings dialog
            await _dialogService.ShowMessageAsync("Storage Settings", "Storage settings dialog will be implemented in the next phase.");
            
            StatusMessage = "Storage settings opened successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error opening storage settings: {ex.Message}");
            await _dialogService.ShowErrorAsync("Settings Error", $"Failed to open storage settings: {ex.Message}");
            StatusMessage = "Error opening storage settings";
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
                    _fileItems.Add(new StorageFileItem
                    {
                        Name = System.IO.Path.GetFileName(file),
                        Path = file,
                        Size = new System.IO.FileInfo(file).Length,
                        Type = System.IO.Path.GetExtension(file),
                        LastModified = System.IO.File.GetLastWriteTime(file),
                        Status = "Pending",
                        IsSelected = false,
                        IsSystem = false,
                        IsTemporary = false,
                        IsDuplicate = false
                    });
                }
                
                await _loggerService.LogAsync($"Added {result.Length} files to cleaning queue");
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
                    _fileItems.Add(new StorageFileItem
                    {
                        Name = System.IO.Path.GetFileName(file),
                        Path = file,
                        Size = new System.IO.FileInfo(file).Length,
                        Type = System.IO.Path.GetExtension(file),
                        LastModified = System.IO.File.GetLastWriteTime(file),
                        Status = "Pending",
                        IsSelected = false,
                        IsSystem = (System.IO.File.GetAttributes(file) & System.IO.FileAttributes.System) == System.IO.FileAttributes.System,
                        IsTemporary = false,
                        IsDuplicate = false
                    });
                }
                
                await _loggerService.LogAsync($"Added {files.Length} files from folder '{result}' to cleaning queue");
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
                "Are you sure you want to clear all files from the cleaning queue?");
            
            if (result)
            {
                StatusMessage = "Clearing all files...";
                
                _fileItems.Clear();
                
                await _loggerService.LogAsync("Cleared all files from cleaning queue");
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

    private async void CleanFile(StorageFileItem file)
    {
        try
        {
            StatusMessage = $"Cleaning file '{file.Name}'...";
            
            // Clean individual file
            await _storageService.CleanFileAsync(file.Path, SelectedCleanLevel, SelectedCleanMode);
            
            // Update file status
            file.Status = "Cleaned";
            
            await _loggerService.LogAsync($"File '{file.Name}' cleaned successfully");
            StatusMessage = "File cleaned successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error cleaning file: {ex.Message}");
            await _dialogService.ShowErrorAsync("Clean File Error", $"Failed to clean file: {ex.Message}");
            StatusMessage = "Error cleaning file";
        }
    }

    private async void RemoveFile(StorageFileItem file)
    {
        try
        {
            StatusMessage = $"Removing file '{file.Name}'...";
            
            // Remove file from queue
            _fileItems.Remove(file);
            
            await _loggerService.LogAsync($"File '{file.Name}' removed from cleaning queue");
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
                case "quick":
                    await ApplyQuickPresetAsync();
                    break;
                case "standard":
                    await ApplyStandardPresetAsync();
                    break;
                case "aggressive":
                    await ApplyAggressivePresetAsync();
                    break;
                case "custom":
                    await ApplyCustomPresetAsync();
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

    private async Task ApplyQuickPresetAsync()
    {
        // Set quick clean settings
        SelectedCleanLevel = "Quick";
        SelectedCleanMode = "Batch";
        
        // Enable minimal cleaning
        CleanTemporaryFiles = true;
        CleanBrowserCache = false;
        CleanDownloadHistory = false;
        CleanRecycleBin = false;
        CleanSystemLogs = false;
        CleanApplicationData = false;
        
        await _loggerService.LogAsync("Quick clean preset applied");
    }

    private async Task ApplyStandardPresetAsync()
    {
        // Set standard clean settings
        SelectedCleanLevel = "Standard";
        SelectedCleanMode = "Batch";
        
        // Enable balanced cleaning
        CleanTemporaryFiles = true;
        CleanBrowserCache = true;
        CleanDownloadHistory = true;
        CleanRecycleBin = true;
        CleanSystemLogs = false;
        CleanApplicationData = false;
        
        await _loggerService.LogAsync("Standard clean preset applied");
    }

    private async Task ApplyAggressivePresetAsync()
    {
        // Set aggressive clean settings
        SelectedCleanLevel = "Aggressive";
        SelectedCleanMode = "Recursive";
        
        // Enable maximum cleaning
        CleanTemporaryFiles = true;
        CleanBrowserCache = true;
        CleanDownloadHistory = true;
        CleanRecycleBin = true;
        CleanSystemLogs = true;
        CleanApplicationData = true;
        
        await _loggerService.LogAsync("Aggressive clean preset applied");
    }

    private async Task ApplyCustomPresetAsync()
    {
        // Set custom clean settings
        SelectedCleanLevel = "Maximum";
        SelectedCleanMode = "Scheduled";
        
        // Enable user-selected cleaning
        // Settings are already bound to UI controls
        
        await _loggerService.LogAsync("Custom clean preset applied");
    }

    private async void ApplyAdvancedSettings()
    {
        try
        {
            StatusMessage = "Applying advanced settings...";
            
            // Apply advanced storage settings
            await _storageService.SetStorageSettingsAsync(
                CleanTemporaryFiles,
                CleanBrowserCache,
                CleanDownloadHistory,
                CleanRecycleBin,
                CleanSystemLogs,
                CleanApplicationData,
                EnableAutomaticCleaning,
                CleanOnLowDiskSpace,
                CleanScheduledTasks,
                CleanSystemRestorePoints,
                IntegrateWithFileExplorer,
                EnableNotifications,
                EnableLogging,
                EnableBackup);
            
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

    private string GetStorageStats()
    {
        try
        {
            var totalFiles = FileItems.Count;
            var selectedFiles = FileItems.Count(f => f.IsSelected);
            var totalSize = FileItems.Sum(f => f.Size);
            var selectedSize = FileItems.Where(f => f.IsSelected).Sum(f => f.Size);
            
            return $"Total: {totalFiles} files ({totalSize / 1024 / 1024:F1} MB), Selected: {selectedFiles} files ({selectedSize / 1024 / 1024:F1} MB)";
        }
        catch
        {
            return "No storage data available";
        }
    }

    private object GetFileTypeDistribution()
    {
        try
        {
            // Return file type distribution as a list
            var distribution = FileItems.GroupBy(f => f.Type)
                .OrderByDescending(g => g.Sum(f => f.Size))
                .Take(10)
                .Select(g => new { Type = g.Key, Count = g.Count(), Size = g.Sum(f => f.Size) })
                .ToList();
            
            return distribution;
        }
        catch
        {
            return new List<object>();
        }
    }

    private object GetLargeFiles()
    {
        try
        {
            // Return large files as a list
            var largeFiles = FileItems.Where(f => f.Size > 100 * 1024 * 1024) // >100MB
                .OrderByDescending(f => f.Size)
                .Take(10)
                .ToList();
            
            return largeFiles;
        }
        catch
        {
            return new List<StorageFileItem>();
        }
    }

    private object GetDuplicateFiles()
    {
        try
        {
            // Return duplicate files as a list
            var duplicates = FileItems.Where(f => f.IsDuplicate)
                .Take(10)
                .ToList();
            
            return duplicates;
        }
        catch
        {
            return new List<StorageFileItem>();
        }
    }

    private object GetTemporaryFiles()
    {
        try
        {
            // Return temporary files as a list
            var tempFiles = FileItems.Where(f => f.IsTemporary)
                .Take(10)
                .ToList();
            
            return tempFiles;
        }
        catch
        {
            return new List<StorageFileItem>();
        }
    }

    private object GetRecentCleans()
    {
        try
        {
            // Return recent cleans as a list
            return StorageHistory.Take(10).ToList();
        }
        catch
        {
            return new List<StorageHistoryItem>();
        }
    }

    private object GetStorageStatistics()
    {
        try
        {
            // Return storage statistics
            return new
            {
                TotalCleans = StorageHistory.Count,
                TotalFilesCleaned = StorageHistory.Sum(c => c.FilesCleaned),
                TotalSpaceReclaimed = StorageHistory.Sum(c => c.SpaceReclaimed),
                AverageSpaceReclaimed = StorageHistory.Any() ? StorageHistory.Average(c => c.SpaceReclaimed) : 0,
                MostUsedCleanLevel = StorageHistory.GroupBy(c => c.CleanLevel)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?
                    .Key ?? "None"
            };
        }
        catch
        {
            return new { TotalCleans = 0, TotalFilesCleaned = 0, TotalSpaceReclaimed = 0, AverageSpaceReclaimed = 0, MostUsedCleanLevel = "None" };
        }
    }

    // Event Handlers
    private void OnMetricsUpdated(object? sender, MetricsUpdatedEventArgs e)
    {
        _ = UpdateStorageDataAsync();
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
            OnPropertyChanged(nameof(StorageStats));
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
        _ = UpdateStorageDataAsync();
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
            await UpdateStorageDataAsync();
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

    private void OnStorageProgressChanged(object? sender, StorageProgressEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Update storage progress
            OnPropertyChanged(nameof(CurrentProgress));
            OnPropertyChanged(nameof(QueueProgress));
            OnPropertyChanged(nameof(ProgressDetails));
            OnPropertyChanged(nameof(EstimatedTimeRemaining));
        });
    }

    private void OnStorageCompleted(object? sender, StorageCompletedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _loggerService.LogAsync($"Storage clean completed: {e.FilesCleaned} files cleaned, {e.SpaceReclaimed} MB reclaimed");
            StatusMessage = $"Storage clean completed: {e.FilesCleaned} files cleaned, {e.SpaceReclaimed} MB reclaimed";
            
            // Update file status
            foreach (var file in FileItems)
            {
                if (file.IsSelected)
                {
                    file.Status = "Cleaned";
                }
            }
        });
    }

    private void OnStorageError(object? sender, StorageErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _loggerService.LogErrorAsync($"Storage error: {e.Message}");
            await _dialogService.ShowErrorAsync("Storage Error", e.Message);
            StatusMessage = $"Storage error: {e.Message}";
        });
    }

    public async Task OnThemeChangedAsync(ThemeVariant newTheme)
    {
        try
        {
            await _loggerService.LogAsync($"Storage theme changed to {newTheme}");
            await UpdateStorageDataAsync();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error handling theme change: {ex.Message}");
        }
    }
}

// Supporting classes
public class StorageFileItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public long Size { get; set; }
    public string Type { get; set; } = "";
    public DateTime LastModified { get; set; }
    public string Status { get; set; } = "";
    public bool IsSelected { get; set; }
    public bool IsSystem { get; set; }
    public bool IsTemporary { get; set; }
    public bool IsDuplicate { get; set; }
}

public class StorageHistoryItem
{
    public DateTime CleanDate { get; set; }
    public int FilesCleaned { get; set; }
    public double SpaceReclaimed { get; set; }
    public string CleanLevel { get; set; } = "";
    public string Status { get; set; } = "";
}