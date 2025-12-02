# ACPI Safety System - Usage Examples

This document shows how to use the new safety system to prevent hardware configuration issues.

---

## Basic Usage

### Before (Unsafe)

```csharp
// Old way - NO safety checks
using var acpi = new AsusAcpiInterface();
var coreManager = new CoreManager(acpi);

// Dangerous - could brick system if values are wrong!
coreManager.SetCores(6, 8);
```

### After (Safe)

```csharp
// New way - WITH full safety protection
using var safeAcpi = new SafeAcpiInterface(logger);

// Automatic validation, snapshot, and rollback protection
bool success = safeAcpi.SetCores(6, 8, "Optimizing for performance");

if (success)
{
    MessageBox.Show(
        "Core configuration updated.\n\n" +
        "IMPORTANT: After rebooting, if the system is stable, " +
        "run the application and click 'Confirm Stable' to finalize the change.\n\n" +
        "If the system fails to boot, automatic rollback will restore " +
        "the previous configuration on next successful boot.",
        "Reboot Required",
        MessageBoxButton.OK,
        MessageBoxImage.Warning
    );
}
else
{
    MessageBox.Show(
        "Failed to update core configuration. Check logs for details.",
        "Configuration Failed",
        MessageBoxButton.OK,
        MessageBoxImage.Error
    );
}
```

---

## Application Startup Integration

### Add to MainWindow.xaml.cs or Program.cs

```csharp
private SafeAcpiInterface? _safeAcpi;

protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    try
    {
        if (AsusAcpiInterface.IsAvailable())
        {
            _safeAcpi = new SafeAcpiInterface(logger);

            // Check if rollback occurred
            if (_safeAcpi.IsRollbackPending())
            {
                var result = MessageBox.Show(
                    "System instability was detected and configuration was rolled back.\n\n" +
                    "The previous hardware settings have been restored.\n\n" +
                    "Do you want to view the rollback log?",
                    "Automatic Rollback Occurred",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    // Show rollback log window
                    ShowRollbackLog();
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError($"Failed to initialize ACPI interface: {ex.Message}");
    }
}
```

---

## Test Mode (Safe Testing)

### Test hardware changes without actually applying them

```csharp
using var safeAcpi = new SafeAcpiInterface(logger);

// Enable test mode - NO actual writes to hardware
safeAcpi.TestModeEnabled = true;

// These will log what WOULD happen, but won't actually change hardware
safeAcpi.SetCores(4, 6, "Testing reduced core count");
safeAcpi.SetBatteryLimit(80, "Testing battery optimization");
safeAcpi.SetPerformanceMode(1, "Testing performance mode");

// Disable test mode to actually apply changes
safeAcpi.TestModeEnabled = false;
safeAcpi.SetCores(4, 6, "Applying reduced core count");
```

---

## Manual Snapshot Management

### Create named snapshots for different scenarios

```csharp
using var safeAcpi = new SafeAcpiInterface(logger);
var snapshotManager = safeAcpi.GetSnapshotManager();

// Capture factory/default configuration
snapshotManager.CaptureAndSave(acpi.GetRawInterface(), "factory", "Fresh install defaults");

// Capture performance configuration
safeAcpi.SetCores(6, 8);
safeAcpi.SetBatteryLimit(100);
safeAcpi.SetPerformanceMode(2);
snapshotManager.CaptureAndSave(acpi.GetRawInterface(), "performance", "Maximum performance settings");

// Capture battery saver configuration
safeAcpi.SetCores(2, 4);
safeAcpi.SetBatteryLimit(60);
safeAcpi.SetPerformanceMode(0);
snapshotManager.CaptureAndSave(acpi.GetRawInterface(), "battery_saver", "Maximum battery life");

// Later: Restore a specific profile
var snapshot = snapshotManager.LoadSnapshot("performance");
if (snapshot != null)
{
    snapshot.ApplyTo(acpi.GetRawInterface(), logger);
}
```

---

## User Confirmation Dialog

### After reboot, ask user to confirm stability

```csharp
// In Application startup or main window load
private void CheckStability()
{
    if (_safeAcpi?.IsRollbackPending() == true)
    {
        // Create a dialog that auto-shows after boot
        var dialog = new StabilityConfirmationDialog();
        dialog.Owner = this;
        
        var result = dialog.ShowDialog();
        
        if (result == true)
        {
            // User confirmed stable
            _safeAcpi.ConfirmStable();
            MessageBox.Show(
                "Configuration confirmed and saved.",
                "Stability Confirmed",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        else
        {
            // User reported instability - rollback
            _safeAcpi.ManualRollback();
            MessageBox.Show(
                "Configuration has been rolled back to previous stable state.",
                "Rollback Completed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }
    }
}
```

### Example Stability Dialog (WPF)

```xml
<!-- StabilityConfirmationDialog.xaml -->
<Window x:Class="RamOptimizerUI.StabilityConfirmationDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Confirm System Stability"
        Width="500" Height="250"
        WindowStartupLocation="CenterScreen"
        ResizeMode="NoResize">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" FontSize="16" FontWeight="Bold" Margin="0,0,0,15">
            Hardware Configuration Changed
        </TextBlock>

        <TextBlock Grid.Row="1" TextWrapping="Wrap" VerticalAlignment="Center">
            Your hardware configuration was recently changed.
            <LineBreak/><LineBreak/>
            Is your system running <Bold>stable</Bold> with no crashes, freezes, or unusual behavior?
            <LineBreak/><LineBreak/>
            <Run Foreground="Red">If you choose NO, the previous configuration will be restored.</Run>
        </TextBlock>

        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,15,0,0">
            <Button Content="Yes, System is Stable" Width="150" Height="35" 
                    Margin="0,0,10,0" IsDefault="True"
                    Click="ConfirmStable_Click"/>
            <Button Content="No, Rollback" Width="150" Height="35" 
                    IsCancel="True" Click="Rollback_Click"/>
        </StackPanel>
    </Grid>
</Window>
```

```csharp
// StabilityConfirmationDialog.xaml.cs
public partial class StabilityConfirmationDialog : Window
{
    public StabilityConfirmationDialog()
    {
        InitializeComponent();
    }

    private void ConfirmStable_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Rollback_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
```

---

## Gradual Change Application

### Apply changes one at a time to isolate issues

```csharp
public async Task<bool> ApplyChangesGradually(SafeAcpiInterface safeAcpi, 
    int targetPCores, int targetECores, int targetBattery)
{
    var coreManager = new CoreManager(safeAcpi.GetRawInterface());
    var (currentP, currentE) = coreManager.GetCurrentCores();
    var batteryManager = new BatteryManager(safeAcpi.GetRawInterface());
    var currentBattery = batteryManager.GetChargeLimit();

    // Step 1: Change P-cores if needed
    if (currentP != targetPCores)
    {
        logger.LogInformation("Step 1: Changing P-cores");
        
        if (!safeAcpi.SetCores(targetPCores, currentE, "Step 1: P-core adjustment"))
            return false;

        MessageBox.Show(
            "P-cores updated. Please reboot and confirm stability before continuing.",
            "Step 1 Complete",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
        
        return false; // Stop here, require user to reboot and confirm
    }

    // Step 2: Change E-cores if needed
    if (currentE != targetECores)
    {
        logger.LogInformation("Step 2: Changing E-cores");
        
        if (!safeAcpi.SetCores(targetPCores, targetECores, "Step 2: E-core adjustment"))
            return false;

        MessageBox.Show(
            "E-cores updated. Please reboot and confirm stability before continuing.",
            "Step 2 Complete",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
        
        return false; // Stop here
    }

    // Step 3: Change battery limit (no reboot required)
    if (currentBattery != targetBattery)
    {
        logger.LogInformation("Step 3: Changing battery limit");
        
        if (!safeAcpi.SetBatteryLimit(targetBattery, "Step 3: Battery optimization"))
            return false;

        MessageBox.Show(
            "Battery limit updated. All changes complete!",
            "Configuration Complete",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    return true;
}
```

---

## View Snapshots

### List and display all saved snapshots

```csharp
public void ShowSnapshotList()
{
    var snapshotManager = new SnapshotManager(logger: logger);
    var snapshots = snapshotManager.ListSnapshots();

    foreach (var snapshot in snapshots)
    {
        Console.WriteLine(snapshot.ToString());
        // Or display in UI ListBox/DataGrid
    }
}
```

---

## Emergency Recovery

### If system becomes unstable

1. **Boot into Safe Mode** (if Windows boots)
2. **Run this code:**

```csharp
static void Main(string[] args)
{
    var logger = CreateLogger();
    
    try
    {
        if (!AsusAcpiInterface.IsAvailable())
        {
            Console.WriteLine("ACPI interface not available");
            return;
        }

        using var acpi = new AsusAcpiInterface();
        var snapshotManager = new SnapshotManager(logger: logger);

        // Load factory defaults or last stable snapshot
        var snapshot = snapshotManager.LoadSnapshot("factory") 
                    ?? snapshotManager.LoadSnapshot("stable")
                    ?? snapshotManager.LoadLatestSnapshot();

        if (snapshot != null)
        {
            Console.WriteLine($"Restoring: {snapshot}");
            bool success = snapshot.ApplyTo(acpi, logger);
            
            if (success)
            {
                Console.WriteLine("Recovery successful! Reboot your system.");
            }
            else
            {
                Console.WriteLine("Recovery failed. Contact ASUS support.");
            }
        }
        else
        {
            Console.WriteLine("No snapshots found for recovery.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Recovery failed: {ex.Message}");
    }
}
```

---

## Cleanup Old Snapshots

### Prevent snapshot directory from growing too large

```csharp
// Keep only the 10 most recent snapshots
var snapshotManager = new SnapshotManager(logger: logger);
int deleted = snapshotManager.CleanupOldSnapshots(keepCount: 10);
logger.LogInformation($"Cleaned up {deleted} old snapshots");
```

---

## Best Practices

1. ✅ **Always use `SafeAcpiInterface`** instead of direct `AsusAcpiInterface`
2. ✅ **Test with `TestModeEnabled = true`** before applying real changes
3. ✅ **Capture factory defaults** on first run
4. ✅ **Use descriptive change descriptions** for debugging
5. ✅ **Apply changes gradually** - one parameter at a time
6. ✅ **Wait for user confirmation** after each reboot
7. ✅ **Keep emergency recovery** script/tool ready
8. ✅ **Log everything** for post-mortem analysis
9. ✅ **Never set P-cores below 2** or total cores below 4
10. ✅ **Create recovery USB** with BIOS flashback before testing
