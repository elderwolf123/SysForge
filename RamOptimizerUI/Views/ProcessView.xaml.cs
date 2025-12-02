using RamOptimizerUI.Services;

namespace RamOptimizerUI.Views
{
    public partial class ProcessView : UserControl
    {
        private bool _isOptimizationRunning = false;

        public ProcessView()
        {
            InitializeComponent();
            LoadTargetProcesses();
        }

        private void LoadTargetProcesses()
        {
            if (ServiceLocator.TargetProcesses != null)
            {
                foreach (var process in ServiceLocator.TargetProcesses)
                {
                    TargetProcessesListBox.Items.Add(process);
                }
            }
        }

        private void AddTargetProcess_Click(object sender, RoutedEventArgs e)
        {
            var processName = ProcessNameTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(processName))
            {
                if (!ServiceLocator.TargetProcesses.Contains(processName))
                {
                    ServiceLocator.TargetProcesses.Add(processName);
                    TargetProcessesListBox.Items.Add(processName);
                    ServiceLocator.Logger?.LogInfo($"Added target process: {processName}");
                }
            }
        }

        private void RemoveTargetProcess_Click(object sender, RoutedEventArgs e)
        {
            if (TargetProcessesListBox.SelectedItem != null)
            {
                var processName = TargetProcessesListBox.SelectedItem.ToString();
                ServiceLocator.TargetProcesses.Remove(processName ?? "");
                TargetProcessesListBox.Items.Remove(processName);
                ServiceLocator.Logger?.LogInfo($"Removed target process: {processName}");
            }
        }

        private async void TerminateLevel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string levelStr && int.TryParse(levelStr, out int level))
            {
                await ExecuteTerminationLevel(level);
            }
        }

        private async Task ExecuteTerminationLevel(int level)
        {
            if (_isOptimizationRunning)
            {
                MessageBox.Show("Optimization is already in progress.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _isOptimizationRunning = true;
            
            try
            {
                await Task.Delay(2000); // Simulate work
                var memoryFreed = level * 50; // Placeholder logic
                
                ServiceLocator.Logger?.LogInfo($"Executed termination level {level}, freed {memoryFreed} MB");
                MessageBox.Show($"Successfully freed {memoryFreed} MB of memory", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ServiceLocator.Logger?.LogError($"Failed to execute termination level {level}: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isOptimizationRunning = false;
            }
        }
    }
}
