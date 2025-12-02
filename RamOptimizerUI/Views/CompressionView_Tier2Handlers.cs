using System;
using System.Windows;
using System.Windows.Controls;

namespace RamOptimizerUI.Views
{
    public partial class CompressionView
    {
        private void CacheSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CacheSizeText != null)
            {
                double sizeGB = CacheSizeSlider.Value / 1024.0;
                CacheSizeText.Text = $"{sizeGB:F1} GB";
            }
        }

        private void SmartAlgorithmChanged(object sender, RoutedEventArgs e)
        {
            // Refresh tier info when smart algorithm setting changes
        }

        private void Unmount_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_virtualDriveManager?.IsMounted == true)
                {
                    bool success = _virtualDriveManager.Unmount();
                    
                    if (success)
                    {
                        MessageBox.Show(
                            "Virtual file system unmounted successfully!",
                            "Unmount Complete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                        
                        UpdateTier2Status();
                    }
                    else
                    {
                        MessageBox.Show(
                            "Failed to unmount virtual file system.\n\nTry closing any applications accessing the files.",
                            "Unmount Failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error during unmount:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void UpdateTier2Status()
        {
            if (_virtualDriveManager?.IsMounted == true)
            {
                MountedStatusText.Text = $"Mounted at: {_virtualDriveManager.CurrentMountPoint}";
                UnmountButton.IsEnabled = true;

                // Show cache stats if available
                var cacheStats = _virtualDriveManager.GetCacheStatistics();
                if (cacheStats != null)
                {
                    MountedStatusText.Text += $"\nCache: {cacheStats.CurrentSizeMB:F1}/{cacheStats.MaxSizeMB:F1} MB ({cacheStats.CachedFiles} files)";
                }
            }
            else
            {
                MountedStatusText.Text = "No games currently mounted";
                UnmountButton.IsEnabled = false;
            }
        }
    }
}
