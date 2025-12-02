using System.Windows.Input;
using Microsoft.Win32;
using RamOptimizer.Compression;
using RamOptimizer.Compression.Transparent;
using RamOptimizer.Compression.VirtualFS;

namespace RamOptimizerUI.Views
{
    public partial class CompressionView : UserControl
    {
        private StandardModeEngine? _compressionEngine;
        private WindowsCompactCompression? _windowsCompact;
        private VirtualDriveManager? _virtualDriveManager;
        private Tier2Compressor? _tier2Compressor;
        private CompressionTier _selectedTier = CompressionTier.Tier1_WindowsCompact;
        private List<string> _selectedFiles = new();
        private CompressionStatistics _stats = new();
        private string? _tier2CompressedPath;

        public CompressionView()
        {
            InitializeComponent();
            _compressionEngine = new StandardModeEngine();
            _windowsCompact = new WindowsCompactCompression();
            _virtualDriveManager = new VirtualDriveManager();
            _tier2Compressor = new Tier2Compressor();
            LoadStatistics();
            SelectTier1(); // Default to Tier 1
            UpdateTier2Status();
        }

        #region Tier Selection

        private void Tier1_Click(object sender, MouseButtonEventArgs e)
        {
            SelectTier1();
        }

        private void Tier2_Click(object sender, MouseButtonEventArgs e)
        {
            SelectTier2();
        }

        private void Tier3_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(
                "Tier 3: Ultra Archive (~90%) coming soon!\n\n" +
                "This will provide:\n" +
                "• Maximum compression for cold storage\n" +
                "• Auto warm-up to Tier 2 on launch\n" +
                "• Auto recompress on game close\n" +
                "• Perfect for games you play rarely",
                "Future Feature",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void SelectTier1()
        {
            _selectedTier = CompressionTier.Tier1_WindowsCompact;
            
            // Update border styling
            Tier1Border.BorderBrush = (SolidColorBrush)FindResource("AccentBrush");
            Tier1Border.BorderThickness = new Thickness(2);
            Tier2Border.BorderBrush = (SolidColorBrush)FindResource("SubtextBrush");
            Tier2Border.BorderThickness = new Thickness(1);
            Tier3Border.BorderBrush = (SolidColorBrush)FindResource("SubtextBrush");
            Tier3Border.BorderThickness = new Thickness(1);
            
            // Update info panel
            TierInfoTitle.Text = "🟢 Tier 1: Windows Compact";
            TierInfoDescription.Text = "Uses Windows built-in LZX compression. Files remain accessible instantly with no manual mounting. " +
                                      "Perfect for games you play frequently. Safe with anti-cheat systems like EAC and BattlEye.";
            
            // Show/hide tier-specific controls
            SmartAlgorithmCheckbox.Visibility = Visibility.Visible;
            PerFileLearningCheckbox.Visibility = Visibility.Visible;
            if (Tier2SettingsPanel != null) Tier2SettingsPanel.Visibility = Visibility.Collapsed;
        }

        private void SelectTier2()
        {
            _selectedTier = CompressionTier.Tier2_VirtualFS;
            
            // Update border styling
            Tier1Border.BorderBrush = (SolidColorBrush)FindResource("SubtextBrush");
            Tier1Border.BorderThickness = new Thickness(1);
            Tier2Border.BorderBrush = (SolidColorBrush)FindResource("AccentBrush");
            Tier2Border.BorderThickness = new Thickness(2);
            Tier3Border.BorderBrush = (SolidColorBrush)FindResource("SubtextBrush");
            Tier3Border.BorderThickness = new Thickness(1);
            
            // Update info panel
            TierInfoTitle.Text = "🟡 Tier 2: Virtual File System";
            TierInfoDescription.Text = "Uses WinFsp virtual file system with Zstandard level 19 (~75% compression). " +
                                      "Transparent access via junction mounting. Works with Steam/launchers. Requires mount/unmount.";
            
            // Show/hide tier-specific controls
            SmartAlgorithmCheckbox.Visibility = Visibility.Collapsed;
            PerFileLearningCheckbox.Visibility = Visibility.Collapsed;
            if (Tier2SettingsPanel != null) Tier2SettingsPanel.Visibility = Visibility.Visible;
        }

        #endregion

        #region Settings

        private void CompressMediaChanged(object sender, RoutedEventArgs e)
        {
            // Setting automatically applies
        }

        private void MinSavingsChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MinSavingsText != null)
            {
                MinSavingsText.Text = $"{(int)e.NewValue}%";
            }
        }

        #endregion

        #region File Selection

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select File to Compress",
                Multiselect = true,
                Filter = "All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedFiles = dialog.FileNames.ToList();
                FilePathTextBox.Text = _selectedFiles.Count == 1 
                    ? _selectedFiles[0] 
                    : $"{_selectedFiles.Count} files selected";
                CompressButton.IsEnabled = true;
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Folder to Compress",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (Directory.Exists(dialog.SelectedPath))
                {
                    _selectedFiles = Directory.GetFiles(dialog.SelectedPath, "*", SearchOption.AllDirectories).ToList();
                    FilePathTextBox.Text = $"{_selectedFiles.Count} files in folder: {dialog.SelectedPath}";
                    CompressButton.IsEnabled = true;
                }
            }
        }

        #endregion

        #region Compression

        private async void Compress_Click(object sender, RoutedEventArgs e)
        {
            if (!_selectedFiles.Any())
            {
                MessageBox.Show("Please select files or a folder first.", "No Files Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if compressing a folder vs individual files
            bool isFolder = _selectedFiles.Count == 1 && Directory.Exists(_selectedFiles[0]);

            // Show confirmation for Tier 1
            if (_selectedTier == CompressionTier.Tier1_WindowsCompact)
            {
                var result = MessageBox.Show(
                    $"Compress using Windows Compact (Tier 1)?\n\n" +
                    $"Target: {(isFolder ? "Folder" : $"{_selectedFiles.Count} files")}\n" +
                    $"Algorithm: LZX\n" +
                    $"Expected compression: ~55%\n" +
                    $"Access: Fully transparent (no mounting needed)\n\n" +
                    $"Files will remain accessible normally after compression.\n" +
                    $"This is safe for games with anti-cheat systems.",
                    "Confirm Compression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result != MessageBoxResult.Yes)
                    return;
            }

            // Show progress card
            ProgressCard.Visibility = Visibility.Visible;
            CompressButton.IsEnabled = false;
            BrowseFileButton.IsEnabled = false;
            BrowseFolderButton.IsEnabled = false;

            try
            {
                if (_selectedTier == CompressionTier.Tier1_WindowsCompact)
                {
                    await CompressTier1Async();
                }
                else if (_selectedTier == CompressionTier.Tier2_VirtualFS)
                {
                    await CompressTier2Async();
                }
                else
                {
                    // Tier 3 or standard mode
                    await CompressStandardModeAsync();
                }
            }
            finally
            {
                CompressButton.IsEnabled = true;
                BrowseFileButton.IsEnabled = true;
                BrowseFolderButton.IsEnabled = false;
            }
        }

        private async Task CompressTier2Async()
        {
            string sourcePath = _selectedFiles.Count == 1 ? _selectedFiles[0] : Path.GetDirectoryName(_selectedFiles[0])!;
            
            // Ask for game name
            string? gameName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter game name for tracking:",
                "Game Name",
                Path.GetFileName(sourcePath)
            );

            if (string.IsNullOrWhiteSpace(gameName))
            {
                MessageBox.Show("Game name is required for Tier 2 compression.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Determine compressed storage path
            string storageRoot = Path.Combine(Path.GetDirectoryName(sourcePath)!, ".tier2");
            _tier2CompressedPath = Path.Combine(storageRoot, gameName);

            try
            {
                CurrentFileText.Text = "Compressing with Zstandard level 19...";
                StatusText.Text = "Status: Initializing Tier 2 compression";
                CompressionProgressBar.IsIndeterminate = true;

                long cacheSizeMB = (long)CacheSizeSlider.Value;

                // Compress to Tier 2
                var result = await _tier2Compressor!.CompressToTier2Async(
                    sourcePath,
                    _tier2CompressedPath,
                    gameName,
                    zstdLevel: 19
                );

                CompressionProgressBar.IsIndeterminate = false;

                if (result.Success)
                {
                    // Update statistics
                    _stats.TotalFiles += result.FilesCompressed;
                    _stats.TotalSpaceSaved += (result.OriginalSize - result.CompressedSize);
                    _stats.TotalOriginalSize += result.OriginalSize;
                    _stats.TotalCompressedSize += result.CompressedSize;
                    SaveStatistics();
                    UpdateStatisticsDisplay();

                    FilesProcessedText.Text = $"{result.FilesCompressed} / {result.TotalFiles}";
                    SpaceSavedText.Text = FormatSize(result.OriginalSize - result.CompressedSize);
                    CompressionRatioText.Text = $"{result.SpaceSaved:P0}";
                    StatusText.Text = "Status: Mounting virtual file system...";

                    // Auto-mount
                    bool mounted = _virtualDriveManager!.Mount(
                        _tier2CompressedPath,
                        sourcePath + "_virtual",
                        cacheSizeMB
                    );

                    if (mounted)
                    {
                        // Create junction at original path
                        // Note: This would require moving/renaming the original folder first
                        StatusText.Text = "Status: Complete! Game mounted.";
                        UpdateTier2Status();

                        MessageBox.Show(
                            $"Tier 2 compression complete!\n\n" +
                            $"Files compressed: {result.FilesCompressed}\n" +
                            $"Files failed: {result.FilesFailed}\n" +
                            $"Space saved: {FormatSize(result.OriginalSize - result.CompressedSize)} ({result.SpaceSaved:P2})\n\n" +
                            $"Virtual file system mounted!\n" +
                            $"Cache size: {cacheSizeMB / 1024.0:F1} GB\n\n" +
                            $"⚠ Important: Original files are still present.\n" +
                            $"After testing, you can delete the original folder to free space.",
                            "Compression Complete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                    else
                    {
                        StatusText.Text = "Status: Compression complete, mount failed";
                        MessageBox.Show(
                            $"Compression successful but mounting failed.\n\n" +
                            $"Compressed storage: {_tier2CompressedPath}\n" +
                            $"You can try mounting manually later.",
                            "Mount Failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }
                }
                else
                {
                    StatusText.Text = $"Error: {result.Error}";
                    MessageBox.Show(
                        $"Tier 2 compression failed:\n{result.Error}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                MessageBox.Show(
                    $"Tier 2 compression failed:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async Task CompressTier1Async()
        {
            string targetPath = _selectedFiles.Count == 1 ? _selectedFiles[0] : Path.GetDirectoryName(_selectedFiles[0])!;
            bool recursive = Directory.Exists(targetPath);
            
            CompactAlgorithm selectedAlgorithm = CompactAlgorithm.LZX; // Default

            try
            {
                // Smart algorithm selection?
                if (SmartAlgorithmCheckbox.IsChecked == true)
                {
                    CurrentFileText.Text = "Testing algorithms to find the best...";
                    StatusText.Text = "Status: Benchmarking XPRESS4K, XPRESS8K, XPRESS16K, LZX";
                    CompressionProgressBar.IsIndeterminate = true;

                    var selector = new SmartAlgorithmSelector();
                    selectedAlgorithm = await selector.SelectBestAlgorithmAsync(
                        targetPath,
                        sampleSize: 10,
                        prioritizeRatio: true // Prioritize compression ratio over speed
                    );

                    CurrentFileText.Text = $"Selected algorithm: {selectedAlgorithm}";
                    StatusText.Text = $"Status: Using {selectedAlgorithm} (best for this data)";
                    
                    // Brief pause so user sees the selection
                    await Task.Delay(1500);
                }

                CurrentFileText.Text = $"Compressing with Windows Compact ({selectedAlgorithm})...";
                StatusText.Text = $"Status: Running compact.exe with {selectedAlgorithm}";
                CompressionProgressBar.IsIndeterminate = true;

                var result = await _windowsCompact!.CompressAsync(
                    targetPath,
                    selectedAlgorithm,
                    recursive
                );

                CompressionProgressBar.IsIndeterminate = false;

                if (result.Success)
                {
                    // Update statistics
                    _stats.TotalFiles += (int)result.CompressedFiles;
                    _stats.TotalSpaceSaved += (result.OriginalSize - result.CompressedSize);
                    _stats.TotalOriginalSize += result.OriginalSize;
                    _stats.TotalCompressedSize += result.CompressedSize;
                    _stats.FilesSkipped += (int)result.SkippedFiles;
                    SaveStatistics();
                    UpdateStatisticsDisplay();

                    // Update current session display
                    FilesProcessedText.Text = $"{result.CompressedFiles} / {result.TotalFiles}";
                    SpaceSavedText.Text = FormatSize(result.OriginalSize - result.CompressedSize);
                    CompressionRatioText.Text = $"{result.SpaceSaved:P0}";
                    StatusText.Text = "Status: Complete!";

                    string smartInfo = SmartAlgorithmCheckbox.IsChecked == true 
                        ? $"\n\nSelected algorithm: {selectedAlgorithm}\n(Auto-tested for optimal compression)"
                        : "";

                    MessageBox.Show(
                        $"Windows Compact compression complete!\n\n" +
                        $"Files compressed: {result.CompressedFiles}\n" +
                        $"Files skipped: {result.SkippedFiles}\n" +
                        $"Space saved: {FormatSize(result.OriginalSize - result.CompressedSize)} ({result.SpaceSaved:P2})\n" +
                        $"Time: {result.Duration.TotalSeconds:F1}s{smartInfo}\n\n" +
                        $"Files remain accessible transparently!",
                        "Compression Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    StatusText.Text = $"Error: {result.Error}";
                    MessageBox.Show(
                        $"Compression failed:\n{result.Error}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                MessageBox.Show(
                    $"Compression failed:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async Task CompressStandardModeAsync()
        {
            // Get settings
            var backupPolicy = GetSelectedBackupPolicy();
            bool allowMediaCompression = CompressMediaCheckBox.IsChecked == true;
            double minSavingsThreshold = MinSavingsSlider.Value / 100.0;

            int totalFiles = _selectedFiles.Count;
            int processedFiles = 0;
            long totalSaved = 0;
            int skippedFiles = 0;
            long totalOriginalSize = 0;
            long totalCompressedSize = 0;

            try
            {
                foreach (var file in _selectedFiles)
                {
                    CurrentFileText.Text = $"Processing: {Path.GetFileName(file)}";
                    StatusText.Text = $"Status: Compressing...";

                    try
                    {
                        var result = await _compressionEngine!.CompressFileAsync(
                            file,
                            backupPolicy,
                            specificAlgorithm: null,
                            specificLevel: null,
                            allowMediaCompression: allowMediaCompression,
                            minSavingsThreshold: minSavingsThreshold
                        );

                        if (result.Success)
                        {
                            if (result.Algorithm.StartsWith("Skipped"))
                            {
                                skippedFiles++;
                            }
                            else
                            {
                                totalSaved += (result.OriginalSize - result.CompressedSize);
                                totalOriginalSize += result.OriginalSize;
                                totalCompressedSize += result.CompressedSize;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue
                        StatusText.Text = $"Error: {ex.Message}";
                        skippedFiles++;
                    }

                    processedFiles++;

                    // Update progress
                    CompressionProgressBar.Value = (processedFiles / (double)totalFiles) * 100;
                    FilesProcessedText.Text = $"{processedFiles} / {totalFiles}";
                    SpaceSavedText.Text = FormatSize(totalSaved);
                    
                    if (totalOriginalSize > 0)
                    {
                        double avgRatio = (totalOriginalSize - totalCompressedSize) / (double)totalOriginalSize;
                        CompressionRatioText.Text = $"{avgRatio:P0}";
                    }
                }

                StatusText.Text = "Status: Complete!";

                // Update statistics
                _stats.TotalFiles += (processedFiles - skippedFiles);
                _stats.TotalSpaceSaved += totalSaved;
                _stats.FilesSkipped += skippedFiles;
                SaveStatistics();
                UpdateStatisticsDisplay();

                MessageBox.Show(
                    $"Compression complete!\n\n" +
                    $"Files processed: {processedFiles}\n" +
                    $"Files compressed: {processedFiles - skippedFiles}\n" +
                    $"Files skipped: {skippedFiles}\n" +
                    $"Space saved: {FormatSize(totalSaved)}",
                    "Compression Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Compression failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helpers

        private BackupRetentionPolicy GetSelectedBackupPolicy()
        {
            var tag = (BackupPolicyComboBox.SelectedItem as ComboBoxItem)?.Tag as string;
            return tag switch
            {
                "DeleteImmediately" => BackupRetentionPolicy.DeleteImmediately,
                "Keep1Hour" => BackupRetentionPolicy.Keep1Hour,
                "Keep1Day" => BackupRetentionPolicy.Keep1Day,
                "Keep1Week" => BackupRetentionPolicy.Keep1Week,
                "KeepPermanent" => BackupRetentionPolicy.KeepPermanent,
                _ => BackupRetentionPolicy.DeleteImmediately
            };
        }

        private string FormatSize(long bytes)
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

        #endregion

        #region Statistics

        private void LoadStatistics()
        {
            // Load from settings/file
            // For now, start fresh
            UpdateStatisticsDisplay();
        }

        private void SaveStatistics()
        {
            // Save to settings/file
        }

        private void UpdateStatisticsDisplay()
        {
            TotalFilesText.Text = _stats.TotalFiles.ToString();
            TotalSpaceSavedText.Text = FormatSize(_stats.TotalSpaceSaved);
            FilesSkippedText.Text = _stats.FilesSkipped.ToString();
            
            if (_stats.TotalFiles > 0 && _stats.TotalOriginalSize > 0)
            {
                double avgCompression = (_stats.TotalOriginalSize - _stats.TotalCompressedSize) / (double)_stats.TotalOriginalSize;
                AvgCompressionText.Text = $"{avgCompression:P0}";
            }
        }

        #endregion
    }

    public class CompressionStatistics
    {
        public int TotalFiles { get; set; }
        public long TotalSpaceSaved { get; set; }
        public int FilesSkipped { get; set; }
        public long TotalOriginalSize { get; set; }
        public long TotalCompressedSize { get; set; }
    }
}
