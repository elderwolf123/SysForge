using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RamOptimizerUI.Views
{
    public partial class CompressionView
    {
        #region Testing Tools

        private void CreateTestData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TestResultsText.Text = "Creating test data...";
                TestResultsText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 165, 0)); // Orange

                // Create test data in temp location
                string testDataPath = Path.Combine(Path.GetTempPath(), "Tier2TestData");
                
                if (Directory.Exists(testDataPath))
                {
                    var result = MessageBox.Show(
                        $"Test data folder already exists at:\n{testDataPath}\n\nDelete and recreate?",
                        "Test Data Exists",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        Directory.Delete(testDataPath, recursive: true);
                    }
                    else
                    {
                        _selectedFiles = new List<string> { testDataPath };
                        SelectedFilesText.Text = $"1 folder selected: {Path.GetFileName(testDataPath)}";
                        TestResultsText.Text = "✓ Using existing test data folder";
                        TestResultsText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // Green
                        return;
                    }
                }

                // Generate test data
                string gamePath = Tests.TestDataGenerator.CreateDummyGameFolder(
                    Path.GetTempPath(),
                    "Tier2TestData",
                    sizeMB: 100
                );

                // Auto-select the test folder
                _selectedFiles = new List<string> { gamePath };
                SelectedFilesText.Text = $"1 folder selected: {Path.GetFileName(gamePath)}";

                // Show info
                var dirInfo = new DirectoryInfo(gamePath);
                int fileCount = Directory.GetFiles(gamePath, "*", SearchOption.AllDirectories).Length;
                long totalSize = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);

                TestResultsText.Text = $"✓ Test data created: {fileCount} files, {FormatSize(totalSize)}\nPath: {gamePath}";
                TestResultsText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // Green

                MessageBox.Show(
                    $"Test data created successfully!\n\n" +
                    $"Location: {gamePath}\n" +
                    $"Files: {fileCount}\n" +
                    $"Size: {FormatSize(totalSize)}\n\n" +
                    $"The folder has been auto-selected.\n" +
                    $"You can now compress it with Tier 1 or Tier 2!",
                    "Test Data Ready",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                TestResultsText.Text = $"✗ Failed to create test data: {ex.Message}";
                TestResultsText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // Red
                
                MessageBox.Show(
                    $"Failed to create test data:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async void RunTests_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TestResultsText.Text = "Running automated tests...";
                TestResultsText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 165, 0)); // Orange
                RunTestsButton.IsEnabled = false;
                CreateTestDataButton.IsEnabled = false;

                // Run tests
                var tests = new Tests.Tier2AutomatedTests();
                var results = await tests.RunAllTestsAsync();

                // Display results
                string resultColor = results.FailedTests == 0 ? "Green" : "Red";
                var brush = results.FailedTests == 0 
                    ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80))
                    : new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54));

                TestResultsText.Text = $"Tests: {results.PassedTests}/{results.TotalTests} passed ({results.SuccessRate:P0})";
                TestResultsText.Foreground = brush;

                // Show detailed results
                var detailsSb = new System.Text.StringBuilder();
                detailsSb.AppendLine($"Automated Test Results\n");
                detailsSb.AppendLine($"Total Tests: {results.TotalTests}");
                detailsSb.AppendLine($"Passed: {results.PassedTests} ✓");
                detailsSb.AppendLine($"Failed: {results.FailedTests} ✗");
                detailsSb.AppendLine($"Success Rate: {results.SuccessRate:P1}\n");
                detailsSb.AppendLine("Details:");

                foreach (var test in results.Results)
                {
                    string icon = test.Value ? "✓" : "✗";
                    detailsSb.AppendLine($"  {icon} {test.Key}");
                }

                MessageBox.Show(
                    detailsSb.ToString(),
                    results.FailedTests == 0 ? "All Tests Passed!" : "Some Tests Failed",
                    MessageBoxButton.OK,
                    results.FailedTests == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning
                );
            }
            catch (Exception ex)
            {
                TestResultsText.Text = $"✗ Test execution failed: {ex.Message}";
                TestResultsText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // Red

                MessageBox.Show(
                    $"Test execution failed:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                RunTestsButton.IsEnabled = true;
                CreateTestDataButton.IsEnabled = true;
            }
        }

        #endregion
    }
}
