using System.Collections.ObjectModel;
using System.ServiceProcess;
using System.Threading;
using RamOptimizer.ServiceTesting;

namespace RamOptimizerUI.Views
{
    public partial class ServiceTestingView : UserControl
    {
        private ServiceTestEngine? _testEngine;
        private CancellationTokenSource? _cts;
        private ObservableCollection<ServiceResultViewModel> _results = new();

        public ServiceTestingView()
        {
            InitializeComponent();
            
            ResultsListBox.ItemsSource = _results;
            UpdateServiceCount();
        }

        #region Event Handlers

        private void UpdateServiceCount()
        {
            try
            {
                var allServices = ServiceController.GetServices();
                var skipEssential = SkipEssentialCheckBox?.IsChecked ?? true;
                
                var count = allServices.Count(s => 
                    !EssentialServices.IsEssential(s.ServiceName) || !skipEssential);
                
                if (ServiceCountText != null)
                {
                    ServiceCountText.Text = $"Estimated services to test: {count}";
                }
            }
            catch
            {
                if (ServiceCountText != null)
                {
                    ServiceCountText.Text = "Estimated services to test: Unknown";
                }
            }
        }

        private async void StartTest_Click(object sender, RoutedEventArgs e)
        {
            // Show warning dialog
            var result = MessageBox.Show(
                "⚠️ WARNING: Service Testing\n\n" +
                "This will:\n" +
                "• Stop and restart Windows services\n" +
                "• Create a System Restore Point\n" +
                "• May cause temporary system instability\n" +
                "• May reboot your computer automatically\n\n" +
                "Progress is saved automatically and can resume after reboots.\n\n" +
                "RECOMMENDED: Run in a VM or test machine first.\n\n" +
                "Do you want to proceed?",
                "Service Testing Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // Initialize test engine
                _testEngine = new ServiceTestEngine();
                _cts = new CancellationTokenSource();

                // Wire up events
                _testEngine.StatusChanged += OnStatusChanged;
                _testEngine.ProgressChanged += OnProgressChanged;
                _testEngine.ServiceTested += OnServiceTested;

                // Update UI
                StartTestButton.IsEnabled = false;
                StopTestButton.IsEnabled = true;
                ProgressCard.Visibility = Visibility.Visible;
                HealthCard.Visibility = Visibility.Visible;
                _results.Clear();

                // Start test
                var includeUser = TestUserServicesCheckBox.IsChecked ?? true;
                var includeSystem = TestSystemServicesCheckBox.IsChecked ?? true;
                var skipEssential = SkipEssentialCheckBox.IsChecked ?? true;

                await _testEngine.RunComprehensiveTestAsync(
                    includeUser,
                    includeSystem,
                    skipEssential,
                    _cts.Token
                );

                // Test complete
                MessageBox.Show(
                    "Service testing completed!\n\n" +
                    $"Tested: {TestedCountText.Text}\n" +
                    $"Problematic: {ProblematicCountText.Text}\n" +
                    $"Skipped: {SkippedCountText.Text}",
                    "Test Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Test was cancelled.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during testing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Clean up
                if (_testEngine != null)
                {
                    _testEngine.StatusChanged -= OnStatusChanged;
                    _testEngine.ProgressChanged -= OnProgressChanged;
                    _testEngine.ServiceTested -= OnServiceTested;
                }

                StartTestButton.IsEnabled = true;
                StopTestButton.IsEnabled = false;
            }
        }

        private async void StopTest_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to stop testing?\n\n" +
                "This will:\n" +
                "• Cancel the current test\n" +
                "• Restart all stopped services (rollback)\n" +
                "• Save progress up to this point\n\n" +
                "You can resume later from where you left off.",
                "Stop Testing",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _cts?.Cancel();
                
                if (_testEngine != null)
                {
                    await _testEngine.StopTestAsync();
                }

                MessageBox.Show("Test stopped. All services have been restarted.", "Stopped", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping test: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewBlacklist_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var blacklist = new ServiceBlacklist();
                var entries = blacklist.GetAll();

                if (entries.Count == 0)
                {
                    MessageBox.Show("No services in blacklist.", "Blacklist Empty", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var message = "Blacklisted Services:\n\n";
                foreach (var entry in entries.OrderByDescending(e => e.Impact.IsSignificant()))
                {
                    message += $"• {entry.DisplayName} ({entry.ServiceName})\n";
                    message += $"  Reason: {entry.Reason}\n";
                    message += $"  Impact: RAM {entry.Impact.RAMDeltaMB}MB, CPU {entry.Impact.CPUDeltaPercent:F1}%\n\n";
                }

                MessageBox.Show(message, $"Blacklist ({entries.Count} services)", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading blacklist: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearCheckpoint_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will clear all saved test progress.\n\n" +
                "Testing will start from the beginning next time.\n\n" +
                "Continue?",
                "Clear Progress",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var checkpoint = new CheckpointManager();
                checkpoint.Clear();

                MessageBox.Show("Test progress has been cleared.", "Progress Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
                
                UpdateCheckpointStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing checkpoint: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Test Engine Events

        private void OnStatusChanged(object? sender, string status)
        {
            Dispatcher.Invoke(() =>
            {
                if (StatusText != null)
                {
                    StatusText.Text = status;
                }
            });
        }

        private void OnProgressChanged(object? sender, (int current, int total) progress)
        {
            Dispatcher.Invoke(() =>
            {
                if (TestProgressBar != null)
                {
                    var percentage = progress.total > 0 ? (progress.current * 100.0 / progress.total) : 0;
                    TestProgressBar.Value = percentage;
                    
                    if (ProgressPercentText != null)
                    {
                        ProgressPercentText.Text = $"{percentage:F0}%";
                    }
                    
                    if (ProgressDetailText != null)
                    {
                        ProgressDetailText.Text = $"{progress.current} / {progress.total} services tested";
                    }
                }
            });
        }

        private void OnServiceTested(object? sender, ServiceTestResult result)
        {
            Dispatcher.Invoke(() =>
            {
                // Update counts
                var tested = int.Parse(TestedCountText?.Text ?? "0");
                if (TestedCountText != null)
                    TestedCountText.Text = (tested + 1).ToString();

                if (result.IsProblematic)
                {
                    var problematic = int.Parse(ProblematicCountText?.Text ?? "0");
                    if (ProblematicCountText != null)
                        ProblematicCountText.Text = (problematic + 1).ToString();
                }

                if (result.WasSkipped)
                {
                    var skipped = int.Parse(SkippedCountText?.Text ?? "0");
                    if (SkippedCountText != null)
                        SkippedCountText.Text = (skipped + 1).ToString();
                }

                // Add to results list
                _results.Insert(0, new ServiceResultViewModel(result));

                // Limit list size
                while (_results.Count > 100)
                {
                    _results.RemoveAt(_results.Count - 1);
                }

                // Update current service display
                if (CurrentServiceText != null)
                {
                    CurrentServiceText.Text = $"Last tested: {result.DisplayName}";
                }

                // Update checkpoint status
                UpdateCheckpointStatus();
            });
        }

        private void UpdateCheckpointStatus()
        {
            try
            {
                var checkpoint = new CheckpointManager();
                var checkpointData = checkpoint.GetCheckpoint();

                if (CheckpointStatusText != null)
                {
                    if (checkpointData.CompletedServices.Count > 0)
                    {
                        CheckpointStatusText.Text = $"Last Checkpoint: {checkpointData.CompletedServices.Count} services tested";
                    }
                    else
                    {
                        CheckpointStatusText.Text = "Last Checkpoint: None";
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        #endregion
    }

    #region ViewModel

    public class ServiceResultViewModel
    {
        public string Icon { get; set; }
        public string DisplayName { get; set; }
        public string ImpactSummary { get; set; }

        public ServiceResultViewModel(ServiceTestResult result)
        {
            Icon = result.IsProblematic ? "⚠️" : 
                   result.WasSkipped ? "⏭️" : "✓";
            
            DisplayName = result.DisplayName;
            
            if (result.IsProblematic)
            {
                ImpactSummary = $"{result.Reason} - RAM:{result.Impact.RAMDeltaMB}MB CPU:{result.Impact.CPUDeltaPercent:F1}%";
            }
            else if (result.WasSkipped)
            {
                ImpactSummary = result.SkipReason ?? "Skipped";
            }
            else
            {
                ImpactSummary = "Safe";
            }
        }
    }

    #endregion
}
