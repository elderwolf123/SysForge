# ACPI Safety System - Visual Guide

**Easy-to-understand diagrams showing how the safety system protects your hardware**

---

## 🎯 The Complete Safety Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                    USER WANTS TO CHANGE HARDWARE                     │
│              (e.g., Set P-cores=4, E-cores=6)                       │
└────────────────────────────────┬────────────────────────────────────┘
                                 │
                                 ▼
        ┌────────────────────────────────────────────────────┐
        │   LAYER 1: PRE-FLIGHT VALIDATION                   │
        │   ────────────────────────────────                 │
        │   ✓ P-cores >= 2?                                  │
        │   ✓ Total cores >= 4?                              │
        │   ✓ Not in forbidden list (0x0000, 0x0100)?        │
        │   ✓ Within hardware limits?                        │
        └────────────┬──────────────────────┬────────────────┘
                     │                      │
              ┌──────▼──────┐        ┌─────▼──────┐
              │   INVALID   │        │   VALID    │
              │   ❌ REJECT │        │   ✅ PASS  │
              └──────┬──────┘        └─────┬──────┘
                     │                     │
                     │                     ▼
                     │    ┌────────────────────────────────────────┐
                     │    │ LAYER 2: CAPTURE SNAPSHOT              │
                     │    │ ─────────────────────────              │
                     │    │ Save current config to:                │
                     │    │ C:\ProgramData\RamOptimizer\Backups\   │
                     │    │ snapshot_before_change_TIMESTAMP.json  │
                     │    └────────────────┬───────────────────────┘
                     │                     │
                     │                     ▼
                     │    ┌────────────────────────────────────────┐
                     │    │ LAYER 3: SET ROLLBACK FLAG             │
                     │    │ ──────────────────────────             │
                     │    │ Create file: rollback_needed.flag      │
                     │    │ Set boot_count = 0                     │
                     │    │ Log change description                 │
                     │    └────────────────┬───────────────────────┘
                     │                     │
                     │                     ▼
                     │    ┌────────────────────────────────────────┐
                     │    │ LAYER 4: APPLY ACPI CHANGE             │
                     │    │ ──────────────────────────             │
                     │    │ Write to ATKACPI driver:               │
                     │    │ DeviceSet(CORES_CPU, 0x0406)           │
                     │    └────────────────┬───────────────────────┘
                     │                     │
                     │                     ▼
                     │    ┌────────────────────────────────────────┐
                     │    │ LAYER 5: READ-AFTER-WRITE VERIFY       │
                     │    │ ────────────────────────────────       │
                     │    │ Read back value from ACPI              │
                     │    │ Compare with requested value           │
                     │    └────────┬─────────────────┬─────────────┘
                     │             │                 │
                     │      ┌──────▼──────┐   ┌──────▼──────┐
                     │      │  MISMATCH   │   │   MATCHES   │
                     │      │ ❌ ROLLBACK │   │  ✅ SUCCESS │
                     │      └──────┬──────┘   └──────┬──────┘
                     │             │                 │
                     │             │                 ▼
                     │             │    ┌─────────────────────────┐
                     │             │    │  REBOOT REQUIRED        │
                     │             │    │  User reboots system    │
                     │             │    └────────┬────────────────┘
                     │             │             │
                     │             │             ▼
                     │             │    ┌────────────────────────────────┐
                     │             │    │ LAYER 6: BOOT DETECTION        │
                     │             │    │ ───────────────────────        │
                     │             │    │ On startup, check:             │
                     │             │    │ - rollback_needed.flag exists? │
                     │             │    │ - Increment boot_count         │
                     │             │    └────────┬───────────────────────┘
                     │             │             │
                     │             │      ┌──────▼───────┐
                     │             │      │ boot_count=1 │
                     │             │      │ (first boot) │
                     │             │      └──────┬───────┘
                     │             │             │
                     │             │             ▼
                     │             │    ┌─────────────────────────┐
                     │             │    │ User confirms stability │
                     │             │    │ (or app auto-detects)   │
                     │             │    └──────┬──────────────────┘
                     │             │           │
                     │             │    ┌──────▼───────┬──────────┐
                     │             │    │              │          │
                     │             │ ┌──▼────┐  ┌──────▼──────┐  │
                     │             │ │STABLE │  │  UNSTABLE   │  │
                     │             │ │  ✅   │  │     ❌      │  │
                     │             │ └───┬───┘  └──────┬──────┘  │
                     │             │     │             │          │
                     │             │     │             ▼          │
                     │             │     │    ┌────────────────┐  │
                     │             │     │    │ AUTO-ROLLBACK  │  │
                     │             │     │    │ Restore last   │◄─┘
                     │             │     │    │ snapshot       │
                     │             └─────┼────┤ Clear flag     │
                     │                   │    └────────────────┘
                     │                   │
                     ▼                   ▼
            ┌────────────────┐  ┌────────────────────┐
            │  LOG ERROR     │  │ CLEAR ROLLBACK     │
            │  Return false  │  │ FLAG - COMPLETE!   │
            └────────────────┘  └────────────────────┘
```

---

## 🔄 Snapshot System Flow

```
                    APPLICATION LIFECYCLE
                    
    ┌──────────────────────────────────────────────────┐
    │         Application Startup (Every Boot)          │
    └────────────────────┬─────────────────────────────┘
                         │
                         ▼
         ┌───────────────────────────────┐
         │ Check: rollback_needed.flag?  │
         └────────┬──────────────┬───────┘
                  │ NO           │ YES
                  ▼              ▼
          ┌───────────┐   ┌─────────────────────┐
          │  Normal   │   │ ROLLBACK DETECTED!  │
          │   Start   │   │ Load latest snapshot│
          └─────┬─────┘   │ Restore to hardware │
                │         └──────────┬──────────┘
                │                    │
                ▼                    ▼
    ┌───────────────────────────────────────┐
    │     User Makes Hardware Change        │
    │  (e.g., SetCores, SetBatteryLimit)    │
    └────────────────┬──────────────────────┘
                     │
                     ▼
    ┌─────────────────────────────────────────┐
    │  BEFORE: Capture Current Snapshot       │
    │  ────────────────────────────────       │
    │  {                                      │
    │    "timestamp": "2025-11-29 14:30",     │
    │    "p_cores": 6,                        │
    │    "e_cores": 8,                        │
    │    "battery_limit": 100,                │
    │    "performance_mode": 2                │
    │  }                                      │
    │  Saved to: snapshot_before_xxx.json     │
    └────────────────┬────────────────────────┘
                     │
                     ▼
    ┌─────────────────────────────────────────┐
    │  Apply Change (with safety layers)      │
    └────────────────┬────────────────────────┘
                     │
             ┌───────▼───────┐
             │   SUCCESS?    │
             └───┬───────┬───┘
                 │ NO    │ YES
                 ▼       ▼
         ┌───────────┐  ┌──────────────────┐
         │ Auto-     │  │ Wait for user    │
         │ Restore   │  │ to confirm       │
         │ Snapshot  │  │ after reboot     │
         └───────────┘  └────────┬─────────┘
                                 │
                                 ▼
                        ┌─────────────────┐
                        │ Confirmed Stable│
                        │ Save as new     │
                        │ baseline        │
                        └─────────────────┘
```

---

## 🧪 Test Mode vs Real Mode

```
╔═══════════════════════════════════════════════════════╗
║              TEST MODE (Safe Testing)                  ║
╚═══════════════════════════════════════════════════════╝

    safeAcpi.TestModeEnabled = true;
    
    User Request: SetCores(4, 6)
         │
         ▼
    ┌─────────────────────────────┐
    │ Validation (still runs)     │
    │ ✓ All checks performed      │
    └──────────┬──────────────────┘
               │ ✅ Valid
               ▼
    ┌─────────────────────────────┐
    │ LOG: Would set cores to     │
    │      P=4, E=6 (0x0406)      │
    │ ❌ NO ACTUAL WRITE          │
    └──────────┬──────────────────┘
               │
               ▼
    ┌─────────────────────────────┐
    │ Return: Success (simulated) │
    │ Hardware: UNCHANGED ✅       │
    └─────────────────────────────┘


╔═══════════════════════════════════════════════════════╗
║              REAL MODE (Actual Changes)                ║
╚═══════════════════════════════════════════════════════╝

    safeAcpi.TestModeEnabled = false;
    
    User Request: SetCores(4, 6)
         │
         ▼
    ┌─────────────────────────────┐
    │ Validation                  │
    │ ✓ All checks performed      │
    └──────────┬──────────────────┘
               │ ✅ Valid
               ▼
    ┌─────────────────────────────┐
    │ Capture snapshot            │
    │ Set rollback flag           │
    └──────────┬──────────────────┘
               │
               ▼
    ┌─────────────────────────────┐
    │ ✅ ACTUAL WRITE to ACPI     │
    │ DeviceSet(CORES_CPU, 0x0406)│
    └──────────┬──────────────────┘
               │
               ▼
    ┌─────────────────────────────┐
    │ Verify write succeeded      │
    │ Read back and compare       │
    └──────────┬──────────────────┘
               │
               ▼
    ┌─────────────────────────────┐
    │ Hardware: CHANGED ⚠️         │
    │ Rollback: PROTECTED ✅       │
    └─────────────────────────────┘
```

---

## 🚨 What Happens During Boot Failure

```
    NORMAL SCENARIO                    FAILURE SCENARIO
    ───────────────                    ────────────────
    
    ┌────────────┐                     ┌────────────┐
    │ Make Change│                     │ Make Change│
    │ (cores, etc)│                    │ (BAD VALUE)│
    └─────┬──────┘                     └─────┬──────┘
          │                                  │
          ▼                                  ▼
    ┌────────────┐                     ┌────────────┐
    │  Snapshot  │                     │  Snapshot  │
    │   Saved    │                     │   Saved    │
    └─────┬──────┘                     └─────┬──────┘
          │                                  │
          ▼                                  ▼
    ┌────────────┐                     ┌────────────┐
    │   Reboot   │                     │   Reboot   │
    └─────┬──────┘                     └─────┬──────┘
          │                                  │
          ▼                                  ▼
    ┌────────────┐                     ┌────────────┐
    │  Boots OK  │                     │ WON'T BOOT │
    │     ✅     │                     │  ❌ POST   │
    └─────┬──────┘                     │   FAILS    │
          │                            └─────┬──────┘
          ▼                                  │
    ┌────────────┐                           │
    │  App Runs  │                     User manually
    │ boot_count=│                     fixes BIOS
    │     1      │                     (see recovery)
    └─────┬──────┘                           │
          │                                  ▼
          ▼                            ┌────────────┐
    ┌────────────┐                    │ Boots into │
    │   Reboot   │                    │  Safe Mode │
    │   Again    │                    │ or Recovery│
    └─────┬──────┘                    └─────┬──────┘
          │                                 │
          ▼                                 ▼
    ┌────────────┐                    ┌────────────┐
    │  App Runs  │                    │  App Runs  │
    │ boot_count=│                    │boot_count=1│
    │     2      │                    │(first boot)│
    └─────┬──────┘                    └─────┬──────┘
          │                                 │
          ▼                                 ▼
    ┌────────────┐                    ┌────────────┐
    │   CLEAR    │                    │   DETECT   │
    │  Rollback  │                    │  Rollback  │
    │    Flag    │                    │    Flag    │
    │  SUCCESS!  │                    └─────┬──────┘
    └────────────┘                          │
                                            ▼
                                     ┌────────────┐
                                     │  RESTORE   │
                                     │  Snapshot  │
                                     │  Auto-fix! │
                                     └────────────┘
```

---

## 📊 Forbidden vs Safe Values

```
┌──────────────────────────────────────────────────────┐
│              P/E CORE CONFIGURATION                   │
│         Format: 0x[P-cores][E-cores]                 │
└──────────────────────────────────────────────────────┘

❌ FORBIDDEN (Will Prevent Boot):
┌──────────┬─────────────┬────────────────────────┐
│  Value   │ Meaning     │ Why Forbidden          │
├──────────┼─────────────┼────────────────────────┤
│ 0x0000   │ 0P + 0E     │ NO CORES ENABLED!      │
│ 0x0001   │ 0P + 1E     │ Only 1 E-core          │
│ 0x0100   │ 1P + 0E     │ Only 1 P-core          │
│ 0x0101   │ 1P + 1E     │ Only 2 cores total     │
└──────────┴─────────────┴────────────────────────┘

✅ SAFE (System Will Boot):
┌──────────┬─────────────┬────────────────────────┐
│  Value   │ Meaning     │ Status                 │
├──────────┼─────────────┼────────────────────────┤
│ 0x0204   │ 2P + 4E     │ Minimum safe           │
│ 0x0406   │ 4P + 6E     │ Balanced               │
│ 0x0608   │ 6P + 8E     │ Full (i9-13900H max)   │
│ 0x0600   │ 6P + 0E     │ P-cores only (safe)    │
│ 0x0208   │ 2P + 8E     │ Efficiency focused     │
└──────────┴─────────────┴────────────────────────┘

SAFETY RULES:
┌─────────────────────────────────────────┐
│ ✓ P-cores >= 2                          │
│ ✓ Total cores (P + E) >= 4              │
│ ✓ P-cores <= 6  (for i9-13900H)         │
│ ✓ E-cores <= 8  (for i9-13900H)         │
└─────────────────────────────────────────┘
```

---

## 🔧 How Classes Work Together

```
┌─────────────────────────────────────────────────────┐
│              YOUR APPLICATION CODE                   │
│  using var safeAcpi = new SafeAcpiInterface();      │
│  safeAcpi.SetCores(4, 6);                           │
└───────────────────┬─────────────────────────────────┘
                    │
                    ▼
        ┌───────────────────────────┐
        │   SafeAcpiInterface       │
        │   (Main coordinator)      │
        └─┬─────┬──────┬──────┬─────┘
          │     │      │      │
    ┌─────┘     │      │      └──────┐
    ▼           ▼      ▼             ▼
┌────────┐ ┌─────────┐ ┌──────────┐ ┌─────────────┐
│ Safety │ │Snapshot │ │Rollback  │ │   ASUS      │
│Validator│ │Manager │ │ Manager  │ │   ACPI      │
└────┬───┘ └────┬────┘ └────┬─────┘ │ Interface   │
     │          │           │        └──────┬──────┘
     │          │           │               │
     │          │           │               ▼
     │          │           │        ┌─────────────┐
     │          │           │        │  ATKACPI    │
     │          │           │        │  Driver     │
     │          │           │        └──────┬──────┘
     │          │           │               │
     ▼          ▼           ▼               ▼
┌────────────────────────────────────────────────┐
│              HARDWARE LAYER                    │
│  CPU, Battery, GPU, Performance Modes          │
└────────────────────────────────────────────────┘

CALL FLOW:
1. SafeAcpiInterface receives request
2. AcpiSafetyValidator checks if safe
3. SnapshotManager captures current state
4. SafeModeRollback sets protection flag
5. AsusAcpiInterface writes to ATKACPI driver
6. SafeAcpiInterface verifies write succeeded
7. User reboots
8. On next boot, SafeModeRollback checks stability
```

---

## 📁 File System Layout

```
C:\ProgramData\RamOptimizer\
├── Backups\
│   ├── snapshot_factory_20251129_143000.json       (KEEP FOREVER!)
│   ├── snapshot_before_core_change_20251129_143015.json
│   ├── snapshot_before_battery_change_20251129_143020.json
│   ├── snapshot_stable_20251129_143100.json
│   ├── snapshot_performance_20251129_143200.json
│   └── snapshot_latest.json                        (Auto-updated)
│
└── Safety\
    ├── rollback_needed.flag                        (Exists if change pending)
    ├── boot_count.txt                              (Boot counter)
    └── last_change.txt                             (Description of last change)

SNAPSHOT FILE CONTENTS:
{
  "timestamp": "2025-11-29T14:30:00",
  "p_cores": 6,
  "e_cores": 8,
  "battery_limit": 80,
  "performance_mode": 2,
  "gpu_mode": 1,
  "cpu_name": "Intel(R) Core(TM) i9-13900H",
  "snapshot_name": "factory",
  "notes": "Fresh from ASUS - DO NOT DELETE"
}
```

---

## 🎓 Usage Pattern

```
    ┌─────────────────────────────────────────┐
    │  RECOMMENDED WORKFLOW                    │
    └─────────────────────────────────────────┘
    
    Step 1: TEST MODE
    ─────────────────
    safeAcpi.TestModeEnabled = true;
    safeAcpi.SetCores(4, 6);
    // Review logs - no actual changes
    
    Step 2: APPLY CHANGE
    ────────────────────
    safeAcpi.TestModeEnabled = false;
    bool success = safeAcpi.SetCores(4, 6);
    
    if (success) {
        MessageBox.Show("Change applied. Reboot required.");
    }
    
    Step 3: REBOOT
    ──────────────
    User reboots system
    
    Step 4: VERIFY STABILITY
    ─────────────────────────
    // App auto-checks on startup
    if (safeAcpi.IsRollbackPending()) {
        // Show confirmation dialog
        if (UserConfirmsStable()) {
            safeAcpi.ConfirmStable();
        } else {
            safeAcpi.ManualRollback();
        }
    }
    
    Step 5: DONE!
    ─────────────
    Configuration is now stable and saved
```

---

## 💡 Quick Reference

**Before making ANY hardware change:**
```csharp
✅ Use SafeAcpiInterface (not AsusAcpiInterface)
✅ Test with TestModeEnabled = true first
✅ Check logs for errors
✅ Apply changes one at a time
✅ Reboot and confirm stability
```

**After successful change:**
```csharp
✅ Confirm stability via ConfirmStable()
✅ Optionally save as named snapshot
```

**If something goes wrong:**
```csharp
✅ Auto-rollback will trigger on next boot
✅ Or manually call ManualRollback()
✅ Check logs for what happened
```

**Emergency:**
```csharp
✅ Boot to Safe Mode / Recovery
✅ Run emergency recovery tool
✅ Restore factory snapshot
```

This visual guide should make the system much clearer! 🎯
