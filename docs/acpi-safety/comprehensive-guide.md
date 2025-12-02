# ACPI Safety System - Comprehensive Guide

**Complete documentation for the ACPI hardware safety system on ASUS ROG laptops**

---

## 📖 Table of Contents

1. [Overview](#overview)
2. [The 6-Layer Safety System](#the-6-layer-safety-system)
3. [Visual Diagrams](#visual-diagrams)
4. [Usage Examples](#usage-examples)
5. [Validation Rules](#validation-rules)
6. [Monitoring & Verification](#monitoring--verification)
7. [Bug Fixes Applied](#bug-fixes-applied)
8. [Troubleshooting](#troubleshooting)

---

## Overview

The ACPI Safety System prevents hardware bricking when changing CPU cores, battery limits, or performance modes on ASUS ROG laptops. It wraps dangerous ACPI calls with multiple layers of validation, snapshotting, and automatic rollback.

### Why This Exists

**The Problem:** Incorrectly configuring CPU cores via ACPI can completely brick your laptop, requiring ASUS service center intervention (source: developer's bricked ROG Flow Z13).

**The Solution:** 6 layers of protection that validate, snapshot, monitor, and automatically rollback dangerous changes.

### Key Features

- ✅ Pre-flight validation (block dangerous values)
- ✅ Automatic snapshotting before changes
- ✅ Read-after-write verification  
- ✅ Boot failure detection & auto-rollback
- ✅ Test mode for safe simulation
- ✅ G-Helper ACPI monitoring integration

---

## The 6-Layer Safety System

### Layer 1: Pre-Flight Validation

**What:** Check if requested configuration is safe BEFORE writing to hardware

**Rules checked:**
- P-cores >= 2 (minimum for Windows stability)
- Total cores >= 4 (minimum for system operation)
- Not in forbidden list (0x0000, 0x0100, 0x0001, 0x0101)
- Within hardware limits (e.g., <= 6P, <= 8E for i9-13900H)
- Battery limit 60-100%
- Performance mode 0-2

**Code:**
```csharp
var validation = AcpiSafetyValidator.ValidateCoreConfig(pCores, eCores, maxP, maxE);
if (!validation.IsValid) {
    return false; // Rejected before any hardware contact
}
```

### Layer 2: Snapshot Capture

**What:** Save current hardware configuration before making changes

**Location:** `C:\ProgramData\RamOptimizer\Backups\`

**Snapshot contains:**
```json
{
  "timestamp": "2025-11-30T12:00:00",
  "p_cores": 6,
  "e_cores": 8,
  "battery_limit": 80,
  "performance_mode": 2,
  "gpu_mode": 1,
  "snapshot_name": "before_core_change"
}
```

**Code:**
```csharp
_snapshotManager.CaptureAndSave(controller, "before_change", description);
```

### Layer 3: Rollback Flag

**What:** Mark that a risky change is in progress

**Files created:**
- `rollback_needed.flag` - Signals rollback protection is active
- `boot_count.txt` - Tracks successful boots
- `last_change.txt` - Description of what changed

**Why:** If system fails to boot, this flag triggers automatic restoration

**Code:**
```csharp
_rollback.SetRollbackFlag(description);
```

### Layer 4: ACPI Write

**What:** Actually write to hardware via ATKACPI driver

**Code:**
```csharp
bool result = controller.SetCores(pCores, eCores);
if (!result) {
    _rollback.ClearRollbackFlag(); // Didn't change anything, clear flag
    return false;
}
```

### Layer 5: Read-After-Write Verification

**What:** Immediately read back the value to confirm write succeeded

**Why:** ACPI writes can silently fail

**Code:**
```csharp
Thread.Sleep(AcpiConstants.ACPI_WRITE_DELAY_MS); // Give hardware time
int actualP = controller.GetCurrentPCores();
int actualE = controller.GetCurrentECores();

if (actualP != pCores || actualE != eCores) {
    // Verification failed! Attempt immediate rollback
    controller.SetCores(originalP, originalE);
    _rollback.ClearRollbackFlag();
    return false;
}
```

### Layer 6: Boot Detection & Auto-Rollback

**What:** On every app startup, check if rollback is needed

**Logic:**
```csharp
public bool CheckAndRollback(IHardwareController controller, SnapshotManager snapshotManager) {
    if (!File.Exists(rollbackFlagPath)) return false; // No rollback needed
    
    int bootCount = GetBootCount() + 1;
    SaveBootCount(bootCount);
    
    if (bootCount >= REQUIRED_SUCCESSFUL_BOOTS) {
        // Multiple successful boots = change is stable
        ClearRollbackFlag();
        return false;
    }
    
    // Rollback needed!
    var snapshot = snapshotManager.LoadLatestSnapshot();
    bool success = snapshot.ApplyTo(controller);
    
    if (success) {
        ClearRollbackFlag();
        return true; // Rollback completed
    }
    
    return false; // Rollback failed, manual intervention needed
}
```

---

## Visual Diagrams

See [diagrams.html](diagrams.html) for interactive Mermaid flow charts.

**Diagrams included:**
1. Complete Safety Flow - All 6 layers visualized
2. Class Architecture - How components interact
3. Boot Failure vs Normal Scenario
4. Test Mode vs Real Mode
5. Snapshot System Lifecycle
6. Safe vs Forbidden Core Configurations

---

## Usage Examples

### Basic Usage

```csharp
using var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger();

// Create plugin and controller
var plugin = new AsusRogPlugin();
var rawController = plugin.CreateController();
var safeController = new SafeHardwareController(rawController, logger);

// Test mode first!
safeController.TestModeEnabled = true;
safeController.SetCores(4, 6); // Simulated, no actual write
// Review logs

// Apply for real
safeController.TestModeEnabled = false;
bool success = safeController.SetCores(4, 6);

if (success) {
    Console.WriteLine("Change applied! Reboot required.");
} else {
    Console.WriteLine("Change failed validation or write.");
}
```

### Checking for Rollback on Startup

```csharp
// Call this every time your app starts
if (safeController.IsRollbackPending()) {
    bool rolledBack = safeController.ManualRollback();
    
    if (rolledBack) {
        MessageBox.Show("System was unstable. Configuration restored.");
    }
}
```

### Confirming Stability

```csharp
// After reboot, when user confirms system is stable
safeController.ConfirmStable(); // Clears rollback flag
```

### Battery Limit Example

```csharp
// Set battery charge limit to 80%
bool success = safeController.SetChargeLimit(80);

// Battery changes take effect immediately
// Rollback flag auto-clears after 5 seconds
```

### Performance Mode Example

```csharp
// Set to Turbo mode
bool success = safeController.SetMode(PerformanceMode.Turbo);

// Performance changes are low-risk, no rollback flag
```

---

## Validation Rules

### CPU Core Configuration

**Safe Values:**
| P-Cores | E-Cores | Hex Value | Status |
|---------|---------|-----------|--------|
| 2 | 4 | 0x0204 | Minimum safe |
| 4 | 6 | 0x0406 | Balanced |
| 6 | 8 | 0x0608 | Full (i9-13900H max) |
| 6 | 0 | 0x0600 | P-cores only |
| 2 | 8 | 0x0208 | Efficiency focused |

**Forbidden Values (Will NOT Boot):**
| P-Cores | E-Cores | Hex Value | Why Forbidden |
|---------|---------|-----------|---------------|
| 0 | 0 | 0x0000 | No cores enabled! |
| 0 | 1 | 0x0001 | Only 1 E-core |
| 1 | 0 | 0x0100 | Only 1 P-core |
| 1 | 1 | 0x0101 | Only 2 cores total |

**Validation Logic:**
```csharp
public static (bool IsValid, string ErrorMessage) ValidateCoreConfig(int pCores, int eCores, int maxP, int maxE) {
    if (pCores < 2) return (false, "Must have at least 2 P-cores");
    if (pCores + eCores < 4) return (false, "Must have at least 4 total cores");
    if (IsConfigurationForbidden((pCores << 8) | eCores)) return (false, "Configuration is forbidden");
    // ... more checks
    return (true, string.Empty);
}
```

### Battery Limit

- **Range:** 60-100%
- **Recommended:** 80% for battery longevity
- **Validation:** Rejects values outside 60-100 range

### Performance Mode

- **0:** Silent (lowest power)
- **1:** Balanced/Performance
- **2:** Turbo (highest power)

---

## Monitoring & Verification

### G-Helper Integration

Capture ACPI calls from G-Helper to verify device IDs match:

```csharp
var monitoring = new AcpiMonitoringService(logger);

// Manually capture from Procmon observations
monitoring.CaptureFromGHelper(
    operation: "Set cores to 6P + 8E",
    deviceId: 0x001200D2,
    value: 0x0608
);

// Verify your implementation matches
var result = monitoring.VerifyAgainstCaptures();
if (!result.AllMatch) {
    Console.WriteLine("Device ID mismatch! DO NOT PROCEED");
}
```

### Manual Verification Workflow

1. Install Process Monitor (Procmon)
2. Filter: `Process Name is GHelper.exe` AND `Operation is DeviceIoControl`
3. Open G-Helper and make a change
4. Observe ACPI calls in Procmon
5. Verify device IDs match your implementation
6. Only proceed if values match exactly

---

## Bug Fixes Applied

All 7 identified bugs have been fixed:

### Bug #7 (HIGH): Test Mode Sets Rollback Flag
**Fixed:** Moved rollback flag setting after test mode check
```csharp
// Before: Flag set even in test mode
_rollback.SetRollbackFlag(description);
if (TestModeEnabled) { return true; } // Flag never cleared!

// After: Check test mode first
if (TestModeEnabled) { return true; }
_rollback.SetRollbackFlag(description); // Only set if real mode
```

### Bug #1 (HIGH): Battery Race Condition
**Fixed:** Replaced fire-and-forget with safe Task.Run
```csharp
// Before: No error handling, could fail silently
Task.Delay(5000).ContinueWith(_ => _rollback.ClearRollbackFlag());

// After: Safe background task
_ = Task.Run(async () => {
    try {
        await Task.Delay(AcpiConstants.BATTERY_CONFIRM_DELAY_MS);
        if (!_disposed) _rollback.ClearRollbackFlag();
    } catch (Exception ex) {
        _logger?.LogError($"Failed to clear flag: {ex.Message}");
    }
});
```

### Bug #6 (MEDIUM): Missing Disposal Checks
**Fixed:** Added checks to all public methods
```csharp
public bool SetCores(int pCores, int eCores) {
    if (_disposed) throw new ObjectDisposedException(nameof(SafeHardwareController));
    // ... rest of method
}
```

### Bug #3 (MEDIUM): Null Checks in Rollback
**Fixed:** Added parameter validation
```csharp
public bool CheckAndRollback(IHardwareController controller, SnapshotManager snapshotManager) {
    if (controller == null) {
        _logger?.LogError("Cannot perform rollback: controller is null");
        return false;
    }
    // ... rest of method
}
```

### Bug #2 (MEDIUM): Performance Mode Verification
**Fixed:** Added read-after-write verification
```csharp
bool result = perfCtrl.SetMode(mode);
if (!result) return false;

Thread.Sleep(AcpiConstants.ACPI_WRITE_DELAY_MS);
var currentMode = perfCtrl.GetCurrentMode();
if ((int)currentMode != modeInt) {
    _logger?.LogWarning("Verification mismatch");
}
```

### Bug #5 (LOW): Snapshot Order Issue
**Fixed:** Set rollback flag BEFORE snapshot
```csharp
// Before: Snapshot first (crash between = wrong snapshot used)
_snapshotManager.CaptureAndSave(...);
_rollback.SetRollbackFlag(...);

// After: Flag first
_rollback.SetRollbackFlag(...);
_snapshotManager.CaptureAndSave(...);
```

### Bug #4 (LOW): Magic Numbers
**Fixed:** Created `AcpiConstants.cs`
```csharp
public static class AcpiConstants {
    public const int ACPI_WRITE_DELAY_MS = 100;
    public const int ACPI_VERIFY_DELAY_MS = 200;
    public const int BATTERY_CONFIRM_DELAY_MS = 5000;
    public const int REQUIRED_SUCCESSFUL_BOOTS = 2;
}
```

---

## Troubleshooting

### System Won't Boot After Change

**This is what the safety system prevents!** But if it happens:

1. **Boot to Safe Mode or Recovery**
   - Hold F8 during POST
   - Select "Safe Mode"

2. **Run the app in Safe Mode**
   - Rollback will automatically detect and restore

3. **If app won't run:**
   - Manually delete: `C:\ProgramData\RamOptimizer\Safety\rollback_needed.flag`
   - Boot normally
   - Manually restore BIOS settings

### Rollback Not Triggering

**Check:**
- Is `rollback_needed.flag` present?
- Is app running on startup?
- Check logs at `C:\ProgramData\RamOptimizer\logs\`

### Change Not Taking Effect

**Likely causes:**
1. Not rebooted yet (core changes require reboot)
2. Write failed validation
3. ACPI driver not loaded

**Check logs:**
```csharp
_logger.LogInformation("..."); // Check what happened
```

### Emergency Recovery

If all else fails:

1. Boot to ASUS BIOS Recovery Mode
2. Restore factory BIOS settings
3. Reinstall RamOptimizer
4. Start fresh with factory snapshot

---

## File Locations

```
C:\ProgramData\RamOptimizer\
├── Backups\
│   ├── snapshot_factory_TIMESTAMP.json         ← Keep this forever!
│   ├── snapshot_before_change_TIMESTAMP.json
│   └── snapshot_stable_TIMESTAMP.json
│
├── Safety\
│   ├── rollback_needed.flag                   ← Triggers rollback
│   ├── boot_count.txt                         ← Boot counter
│   └── last_change.txt                        ← What changed
│
└── logs\                                      ← Application logs
```

---

## Additional Resources

- [MODULAR_ARCHITECTURE.md](../../architecture/MODULAR_ARCHITECTURE.md) - Plugin system design
- [BUG_REPORT.md](BUG_REPORT.md) - All bugs found and fixed
- [diagrams.html](diagrams.html) - Visual flow charts
- [ROG Flow Z13 Recovery](../../critical/ROG_FLOW_Z13_BIOS_RECOVERY.md) - BIOS recovery guide

---

## Summary

The ACPI Safety System provides comprehensive protection when modifying hardware via ACPI:

✅ **Validation** - Blocks dangerous values before they reach hardware  
✅ **Snapshotting** - Always have a restore point  
✅ **Verification** - Confirm writes actually worked  
✅ **Rollback** - Auto-restore if system becomes unstable  
✅ **Test Mode** - Safe simulation before real changes  
✅ **Monitoring** - Verify against G-Helper's proven values

**Use this system for ALL hardware changes. It exists because the developer learned the hard way that ACPI can brick your laptop.**

⚠️ **Current Status:** Primary development laptop is bricked and at ASUS for repair. This safety system is the result of that painful lesson.
