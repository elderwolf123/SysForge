using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using RamOptimizer.Compression.HyperCompress;
using Microsoft.Extensions.Logging;

namespace RamOptimizerUI.Views;

/// <summary>
/// Event handlers for Tier 3 (HyperCompress) functionality.
/// </summary>
public partial class CompressionView : UserControl
{
    private HyperCompressEngine? _hyperEngine;
    private ChunkedArchiver? _archiver;
    private string? _tier3SourceFolder;
    private string? _tier3ArchivePath;
    
    private void InitializeTier3()
    {
        // Create HyperCompress engine with all encoders
        _hyperEngine = new HyperCompressEngine();
        _hyperEngine.RegisterEncoder(new Encoders.FallbackLZ4Encoder());
        _hyperEngine.RegisterEncoder(new Encoders.HyperGameTextureEncoder());
        _hyperEngine.RegisterEncoder(new Encoders.HyperGameAudioEncoder());
        _hyperEngine.RegisterEncoder(new Encoders.HyperGameExecutableEncoder());
        _hyperEngine.RegisterEncoder(new Encoders.HyperGeneralEncoder());
        
        _archiver = new ChunkedArchiver(_hyperEngine);
    }
    
    private async void Tier3Compress_Click(object sender, RoutedEventArgs e)
    {
        if (_tier3SourceFolder == null)
        {
            MessageBox.Show("Please select a source folder first", "No Folder Selected");
            return;
        }
        
        // Ask for output location
        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "HyperCompressed Archive (*.hca)|*.hca",
            Title = "Save Compressed Archive",
            FileName = Path.GetFileName(_tier3SourceFolder) + ".hca"
        };
        
        if (saveDialog.ShowDialog() != true) return;
        
        _tier3ArchivePath = saveDialog.FileName;
        
        try
        {
            Tier3CompressButton.IsEnabled = false;
            Tier3StatusText.Text = "Compressing...";
            
            var progress = new Progress<ArchiveProgress>(p =>
            {
                Tier3ProgressBar.Value = p.PercentComplete;
                Tier3StatusText.Text = $"Compressing: {p.FilesProcessed}/{p.TotalFiles} files ({p.PercentComplete:F1}%)";
            });
            
            var settings = new CompressionSettings
            {
                Level = (int)Tier3CompressionLevelSlider.Value
            };
            
            var result = await _archiver!.CreateArchiveAsync(
                _tier3SourceFolder,
                _tier3ArchivePath,
                settings,
                progress);
            
            if (result.Success)
            {
                double savedMB = (result.OriginalSize - result.CompressedSize) / (1024.0 * 1024.0);
                Tier3StatusText.Text = $"Complete! {result.TotalFiles} files, " +
                    $"saved {savedMB:F1} MB ({result.CompressionRatio:P2} ratio)";
                
                Tier3ExtractButton.IsEnabled = true;
            }
            else
            {
                MessageBox.Show($"Compression failed: {result.ErrorMessage}", "Error");
                Tier3StatusText.Text = "Compression failed";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Compression Error");
            Tier3StatusText.Text = "Error occurred";
        }
        finally
        {
            Tier3CompressButton.IsEnabled = true;
            Tier3ProgressBar.Value = 0;
        }
    }
    
    private void Tier3BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select folder to compress",
            ShowNewFolderButton = false
        };
        
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _tier3SourceFolder = dialog.SelectedPath;
            Tier3FolderText.Text = _tier3SourceFolder;
            Tier3CompressButton.IsEnabled = true;
        }
    }
    
    private async void Tier3Extract_Click(object sender, RoutedEventArgs e)
    {
        if (_tier3ArchivePath == null || !File.Exists(_tier3ArchivePath))
        {
            MessageBox.Show("No archive to extract", "Error");
            return;
        }
        
        var folderDialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select extraction destination",
            ShowNewFolderButton = true
        };
        
        if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
        
        try
        {
            Tier3ExtractButton.IsEnabled = false;
            Tier3StatusText.Text = "Extracting...";
            
            var reader = new ChunkedArchiveReader(_tier3ArchivePath, _hyperEngine!);
            reader.Open();
            
            var fileCount = reader.GetFileList().Count;
            var progress = new Progress<int>(n =>
            {
                Tier3ProgressBar.Value = (double)n / fileCount * 100;
                Tier3StatusText.Text = $"Extracting: {n}/{fileCount} files";
            });
            
            await Task.Run(() => reader.ExtractAll(folderDialog.SelectedPath, progress));
            
            Tier3StatusText.Text = $"Extracted {fileCount} files successfully!";
            reader.Dispose();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Extraction Error");
            Tier3StatusText.Text = "Extraction failed";
        }
        finally
        {
            Tier3ExtractButton.IsEnabled = true;
            Tier3ProgressBar.Value = 0;
        }
    }
}
