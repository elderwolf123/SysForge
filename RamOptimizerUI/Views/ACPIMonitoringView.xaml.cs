using System.Collections.ObjectModel;
using RamOptimizer.HardwareControl.Monitoring;

namespace RamOptimizerUI.Views
{
    public partial class ACPIMonitoringView : UserControl
    {
        private AcpiMonitoringService _monitoringService;
        private ObservableCollection<AcpiTransactionViewModel> _transactions = new();

        public ACPIMonitoringView()
        {
            InitializeComponent();
            
            // Initialize monitoring service
            _monitoringService = new AcpiMonitoringService(null); // TODO: Add proper logging
            
            // Setup DataGrid
            TransactionsDataGrid.ItemsSource = _transactions;
            
            // Load device IDs
            LoadDeviceIdReferences();
            
            // Auto-load captures if they exist
            LoadCapturesFromService();
        }

        private void LoadDeviceIdReferences()
        {
            // Display current device IDs from AsusAcpiInterface
            PerfModeDeviceIdText.Text = $"0x{AsusAcpiInterface.PerformanceMode:X8}";
            GpuModeDeviceIdText.Text = $"0x{AsusAcpiInterface.GpuMode:X8}";
            CoresDeviceIdText.Text = $"0x{AsusAcpiInterface.CORES_CPU:X8}";
            BatteryLimitDeviceIdText.Text = $"0x{AsusAcpiInterface.BatteryLimit:X8}";
            FanCurveDeviceIdText.Text = $"0x{AsusAcpiInterface.FAN_CURVE:X8}";
            TurboModeDeviceIdText.Text = $"0x{AsusAcpiInterface.TURBO_MODE:X8}";
        }

        private void LoadCapturesFromService()
        {
            try
            {
                _monitoringService.LoadCaptures();
                RefreshTransactionsList();
            }
            catch
            {
                // No captures to load
            }
        }

        private void LoadCaptures_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _monitoringService.LoadCaptures();
                RefreshTransactionsList();
                
                MessageBox.Show(
                    $"Loaded {_transactions.Count} captured transactions.",
                    "Captures Loaded",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load captures: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void AddManual_Click(object sender, RoutedEventArgs e)
        {
            // Show dialog to add manual capture
            var dialog = new AddManualCaptureDialog();
            if (dialog.ShowDialog() == true)
            {
                _monitoringService.AddManualCapture(
                    dialog.ProcessName,
                    dialog.DeviceId,
                    dialog.Value,
                    dialog.Description
                );
                
                RefreshTransactionsList();
                _monitoringService.SaveCaptures();
            }
        }

        private void Verify_Click(object sender, RoutedEventArgs e)
        {
            if (!_transactions.Any())
            {
                MessageBox.Show(
                    "No captured transactions to verify.\n\nLoad captures or add manual entries first.",
                    "No Data",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            try
            {
                var result = _monitoringService.VerifyAgainstCaptures();
                
                // Show validation card
                ValidationCard.Visibility = Visibility.Visible;
                
                // Update counts
                TotalCapturedText.Text = result.TotalCaptured.ToString();
                MatchesText.Text = result.Matches.Count.ToString();
                MismatchesText.Text = result.Mismatches.Count.ToString();
                
                // Update status
                if (result.AllMatch)
                {
                    ValidationStatusBorder.Background = new SolidColorBrush(Color.FromRgb(30, 42, 30));
                    ValidationStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    ValidationStatusText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    ValidationStatusText.Text = "✅ All device IDs match! Our implementation is correct.";
                }
                else if (result.Mismatches.Count > 0)
                {
                    ValidationStatusBorder.Background = new SolidColorBrush(Color.FromRgb(42, 30, 30));
                    ValidationStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    ValidationStatusText.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    ValidationStatusText.Text = $"❌ {result.Mismatches.Count} device ID mismatch(es) detected!";
                }
                else
                {
                    ValidationStatusBorder.Background = new SolidColorBrush(Color.FromRgb(42, 38, 30));
                    ValidationStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));
                    ValidationStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0));
                    ValidationStatusText.Text = "⚠️ No matches found - add more captures for verification.";
                }
                
                // Populate details
                ValidationDetailsPanel.Children.Clear();
                
                foreach (var match in result.Matches)
                {
                    var detailText = new TextBlock
                    {
                        Text = "✅ " + match,
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                        Margin = new Thickness(8, 4, 8, 4)
                    };
                    ValidationDetailsPanel.Children.Add(detailText);
                }
                
                foreach (var mismatch in result.Mismatches)
                {
                    var detailText = new TextBlock
                    {
                        Text = "❌ " + mismatch,
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                        Margin = new Thickness(8, 4, 8, 4),
                        TextWrapping = TextWrapping.Wrap
                    };
                    ValidationDetailsPanel.Children.Add(detailText);
                }
                
                // Show success message if all match
                if (result.AllMatch)
                {
                    MessageBox.Show(
                        "Verification Complete!\n\n" +
                        $"✅ All {result.Matches.Count} device IDs match official tools.\n" +
                        "Our ACPI implementation is verified correct.",
                        "Verification Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Verification failed: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Clear all captured transactions?",
                "Confirm Clear",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _monitoringService.ClearCaptures();
                _transactions.Clear();
                TransactionCountText.Text = "0 transactions";
                ValidationCard.Visibility = Visibility.Collapsed;
            }
        }

        private void RefreshTransactionsList()
        {
            _transactions.Clear();
            
            var captures = _monitoringService.GetCaptures();
            foreach (var capture in captures)
            {
                _transactions.Add(new AcpiTransactionViewModel(capture));
            }
            
            TransactionCountText.Text = $"{_transactions.Count} transaction{(_transactions.Count != 1 ? "s" : "")}";
        }
    }

    public class AcpiTransactionViewModel
    {
        public DateTime Timestamp { get; set; }
        public string ProcessName { get; set; }
        public string DeviceIdHex { get; set; }
        public string ValueHex { get; set; }
        public string Description { get; set; }

        public AcpiTransactionViewModel(AcpiMonitoringService.AcpiTransaction transaction)
        {
            Timestamp = transaction.Timestamp;
            ProcessName = transaction.ProcessName;
            DeviceIdHex = $"0x{transaction.DeviceId:X8}";
            ValueHex = $"0x{transaction.Value:X8}";
            Description = transaction.Description;
        }
    }

    // Simple dialog for adding manual captures
    public class AddManualCaptureDialog : Window
    {
        private TextBox _processNameBox;
        private TextBox _deviceIdBox;
        private TextBox _valueBox;
        private TextBox _descriptionBox;

        public string ProcessName => _processNameBox.Text;
        public uint DeviceId => Convert.ToUInt32(_deviceIdBox.Text.Replace("0x", ""), 16);
        public int Value => Convert.ToInt32(_valueBox.Text.Replace("0x", ""), 16);
        public string Description => _descriptionBox.Text;

        public AddManualCaptureDialog()
        {
            Title = "Add Manual Capture";
            Width = 450;
            Height = 350;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Process Name
            grid.Children.Add(CreateLabel("Process Name:", 0));
            _processNameBox = new TextBox { Text = "GHelper.exe", Margin = new Thickness(0, 5, 0, 15) };
            Grid.SetRow(_processNameBox, 0);
            grid.Children.Add(_processNameBox);

            // Device ID
            grid.Children.Add(CreateLabel("Device ID (hex):", 1));
            _deviceIdBox = new TextBox { Text = "0x00120075", Margin = new Thickness(0, 5, 0, 15) };
            Grid.SetRow(_deviceIdBox, 1);
            grid.Children.Add(_deviceIdBox);

            // Value
            grid.Children.Add(CreateLabel("Value (hex):", 2));
            _valueBox = new TextBox { Text = "0x00000000", Margin = new Thickness(0, 5, 0, 15) };
            Grid.SetRow(_valueBox, 2);
            grid.Children.Add(_valueBox);

            // Description
            grid.Children.Add(CreateLabel("Description:", 3));
            _descriptionBox = new TextBox { Text = "Performance Mode - Silent", Margin = new Thickness(0, 5, 0, 15) };
            Grid.SetRow(_descriptionBox, 3);
            grid.Children.Add(_descriptionBox);

            // Buttons
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetRow(buttonPanel, 5);
            
            var okButton = new Button { Content = "Add", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
            okButton.Click += (s, e) => { DialogResult = true; Close(); };
            
            var cancelButton = new Button { Content = "Cancel", Width = 80 };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }

        private TextBlock CreateLabel(string text, int row)
        {
            var label = new TextBlock { Text = text, FontWeight = FontWeights.Bold };
            Grid.SetRow(label, row);
            return label;
        }
    }
}
