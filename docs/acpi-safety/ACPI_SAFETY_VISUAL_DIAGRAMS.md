# ACPI Safety System - Visual Diagrams

**Actual visual flowcharts that render in VS Code, GitHub, and most markdown viewers**

---

## 🎯 Complete Safety Flow

```mermaid
flowchart TD
    Start([User Requests Hardware Change]) --> Validate[Layer 1: Pre-Flight Validation]
    
    Validate --> CheckValid{Valid Parameters?}
    CheckValid -->|No| Reject[❌ Reject & Log Error]
    CheckValid -->|Yes| Snapshot[Layer 2: Capture Snapshot]
    
    Snapshot --> SaveSnapshot[Save to: snapshot_before_xxx.json]
    SaveSnapshot --> RollbackFlag[Layer 3: Set Rollback Flag]
    
    RollbackFlag --> SetFlag[Create rollback_needed.flag<br/>Set boot_count = 0]
    SetFlag --> ApplyChange[Layer 4: Apply ACPI Change]
    
    ApplyChange --> WriteACPI[Write to ATKACPI driver<br/>DeviceSet CORES_CPU, value]
    WriteACPI --> Verify[Layer 5: Read-After-Write Verify]
    
    Verify --> CheckVerify{Value Matches?}
    CheckVerify -->|No| AutoRollback[❌ Immediate Rollback<br/>Restore Original]
    CheckVerify -->|Yes| Reboot[✅ Reboot Required]
    
    Reboot --> UserReboots[User Reboots System]
    UserReboots --> BootDetect[Layer 6: Boot Detection]
    
    BootDetect --> CheckFlag{rollback_needed.flag<br/>exists?}
    CheckFlag -->|No| Normal[Normal Operation]
    CheckFlag -->|Yes| IncrementBoot[Increment boot_count]
    
    IncrementBoot --> CheckCount{boot_count >= 2?}
    CheckCount -->|Yes| ClearFlag[✅ Clear Rollback Flag<br/>SUCCESS!]
    CheckCount -->|No| WaitConfirm[Wait for User Confirmation]
    
    WaitConfirm --> UserConfirm{System Stable?}
    UserConfirm -->|Yes| ClearFlag
    UserConfirm -->|No| RestoreSnapshot[❌ Auto-Restore Snapshot<br/>Rollback Complete]
    
    AutoRollback --> End([End - Failed])
    Reject --> End
    RestoreSnapshot --> End
    ClearFlag --> Success([End - Success])
    Normal --> Success
    
    style Start fill:#4CAF50,stroke:#2E7D32,color:#fff
    style Success fill:#4CAF50,stroke:#2E7D32,color:#fff
    style End fill:#f44336,stroke:#c62828,color:#fff
    style Reject fill:#f44336,stroke:#c62828,color:#fff
    style AutoRollback fill:#FF9800,stroke:#E65100,color:#fff
    style RestoreSnapshot fill:#FF9800,stroke:#E65100,color:#fff
    style Validate fill:#2196F3,stroke:#1565C0,color:#fff
    style Snapshot fill:#9C27B0,stroke:#6A1B9A,color:#fff
    style RollbackFlag fill:#FF9800,stroke:#E65100,color:#fff
    style ApplyChange fill:#FFC107,stroke:#F57F17,color:#000
    style Verify fill:#00BCD4,stroke:#006064,color:#fff
    style BootDetect fill:#9C27B0,stroke:#6A1B9A,color:#fff
    style ClearFlag fill:#4CAF50,stroke:#2E7D32,color:#fff
```

---

## 🔄 Snapshot System Flow

```mermaid
flowchart TD
    AppStart([Application Startup]) --> CheckRollback{Rollback Flag<br/>Exists?}
    
    CheckRollback -->|No| NormalStart[Normal Startup]
    CheckRollback -->|Yes| RollbackDetected[🚨 ROLLBACK DETECTED]
    
    RollbackDetected --> LoadSnapshot[Load Latest Snapshot]
    LoadSnapshot --> RestoreHW[Restore to Hardware]
    RestoreHW --> NotifyUser[Notify User of Rollback]
    
    NormalStart --> UserAction[User Makes Hardware Change]
    NotifyUser --> UserAction
    
    UserAction --> CaptureNow[Capture Current State]
    CaptureNow --> CreateSnapshot[Create Snapshot JSON]
    
    CreateSnapshot --> SnapshotData["
    {
      timestamp: 2025-11-29 14:30,
      p_cores: 6,
      e_cores: 8,
      battery_limit: 80,
      performance_mode: 2
    }
    "]
    
    SnapshotData --> SaveFile[Save to Disk<br/>snapshot_before_xxx.json]
    SaveFile --> ApplyChange[Apply Hardware Change<br/>with Safety Layers]
    
    ApplyChange --> ChangeSuccess{Success?}
    
    ChangeSuccess -->|No| AutoRestore[Auto-Restore Snapshot]
    ChangeSuccess -->|Yes| WaitReboot[Wait for User Reboot]
    
    WaitReboot --> AfterReboot[After Reboot]
    AfterReboot --> CheckStable{System Stable?}
    
    CheckStable -->|No| AutoRestore
    CheckStable -->|Yes| ConfirmStable[Confirm Stable<br/>Save as New Baseline]
    
    AutoRestore --> End([Change Reverted])
    ConfirmStable --> Success([New Configuration Active])
    
    style AppStart fill:#4CAF50,stroke:#2E7D32,color:#fff
    style Success fill:#4CAF50,stroke:#2E7D32,color:#fff
    style End fill:#FF9800,stroke:#E65100,color:#fff
    style RollbackDetected fill:#f44336,stroke:#c62828,color:#fff
    style AutoRestore fill:#FF9800,stroke:#E65100,color:#fff
    style CaptureNow fill:#9C27B0,stroke:#6A1B9A,color:#fff
    style ApplyChange fill:#2196F3,stroke:#1565C0,color:#fff
    style ConfirmStable fill:#4CAF50,stroke:#2E7D32,color:#fff
```

---

## 🧪 Test Mode vs Real Mode

```mermaid
flowchart LR
    subgraph TestMode["TEST MODE (TestModeEnabled = true)"]
        T1[User Request:<br/>SetCores 4, 6] --> T2[Validation<br/>✓ Still Runs]
        T2 --> T3{Valid?}
        T3 -->|Yes| T4[LOG: Would set to 0x0406<br/>❌ NO ACTUAL WRITE]
        T3 -->|No| T5[LOG: Would reject<br/>invalid value]
        T4 --> T6[Return Success<br/>✅ Hardware Unchanged]
        T5 --> T6
    end
    
    subgraph RealMode["REAL MODE (TestModeEnabled = false)"]
        R1[User Request:<br/>SetCores 4, 6] --> R2[Validation<br/>✓ Runs]
        R2 --> R3{Valid?}
        R3 -->|Yes| R4[Capture Snapshot]
        R3 -->|No| R5[❌ Reject]
        R4 --> R6[Set Rollback Flag]
        R6 --> R7[✅ WRITE to ACPI<br/>DeviceSet 0x0406]
        R7 --> R8[Verify Write]
        R8 --> R9[⚠️ Hardware Changed<br/>✅ Rollback Protected]
    end
    
    style TestMode fill:#E8F5E9,stroke:#4CAF50
    style RealMode fill:#FFF3E0,stroke:#FF9800
    style T6 fill:#4CAF50,stroke:#2E7D32,color:#fff
    style R9 fill:#FF9800,stroke:#E65100,color:#fff
    style R5 fill:#f44336,stroke:#c62828,color:#fff
```

---

## 🏗️ Class Architecture

```mermaid
classDiagram
    class SafeAcpiInterface {
        +bool TestModeEnabled
        +SetCores(pCores, eCores)
        +SetBatteryLimit(limit)
        +SetPerformanceMode(mode)
        +ConfirmStable()
        +ManualRollback()
        +GetCurrentSnapshot()
    }
    
    class AcpiSafetyValidator {
        +ValidateCoreConfig()
        +ValidateBatteryLimit()
        +ValidatePerformanceMode()
        +IsConfigurationForbidden()
    }
    
    class SnapshotManager {
        +CaptureAndSave()
        +LoadLatestSnapshot()
        +LoadSnapshot(name)
        +RestoreSnapshot(name)
        +ListSnapshots()
        +CleanupOldSnapshots()
    }
    
    class SafeModeRollback {
        +SetRollbackFlag()
        +ClearRollbackFlag()
        +CheckAndRollback()
        +IsRollbackPending()
        +GetBootCount()
    }
    
    class HardwareSnapshot {
        +int PCores
        +int ECores
        +int BatteryLimit
        +DateTime Timestamp
        +Capture() HardwareSnapshot
        +ApplyTo(acpi)
        +ToJson()
        +FromJson()
    }
    
    class AsusAcpiInterface {
        +DeviceGet(deviceId)
        +DeviceSet(deviceId, value)
        +IsAvailable()
    }
    
    SafeAcpiInterface --> AcpiSafetyValidator : validates with
    SafeAcpiInterface --> SnapshotManager : manages snapshots
    SafeAcpiInterface --> SafeModeRollback : rollback protection
    SafeAcpiInterface --> AsusAcpiInterface : writes to hardware
    SnapshotManager --> HardwareSnapshot : creates/restores
    HardwareSnapshot --> AsusAcpiInterface : captures from / applies to
    
    style SafeAcpiInterface fill:#2196F3,stroke:#1565C0,color:#fff
    style AcpiSafetyValidator fill:#4CAF50,stroke:#2E7D32,color:#fff
    style SnapshotManager fill:#9C27B0,stroke:#6A1B9A,color:#fff
    style SafeModeRollback fill:#FF9800,stroke:#E65100,color:#fff
    style AsusAcpiInterface fill:#f44336,stroke:#c62828,color:#fff
```

---

## 🚨 Boot Failure Scenario

```mermaid
sequenceDiagram
    participant User
    participant App as RamOptimizer App
    participant Safe as SafeAcpiInterface
    participant Snap as SnapshotManager
    participant ACPI as ATKACPI Driver
    participant HW as Hardware/BIOS
    
    User->>App: Change cores to 4P, 6E
    App->>Safe: SetCores(4, 6)
    Safe->>Safe: Validate (✓ Pass)
    Safe->>Snap: Capture current state
    Snap->>ACPI: Read all values
    ACPI-->>Snap: Current config
    Snap->>Snap: Save snapshot_before.json
    Safe->>Safe: Set rollback_needed.flag
    Safe->>ACPI: DeviceSet(CORES_CPU, 0x0406)
    ACPI->>HW: Write to NVRAM
    HW-->>ACPI: ACK
    Safe->>ACPI: DeviceGet(CORES_CPU)
    ACPI-->>Safe: 0x0406 (verified)
    Safe-->>User: Success! Reboot required
    
    Note over User,HW: === REBOOT ===
    
    alt System Boots Successfully
        HW->>HW: POST OK
        HW->>App: Windows starts
        App->>Safe: Startup check
        Safe->>Safe: Check rollback_needed.flag
        Safe->>Safe: boot_count = 1
        Safe-->>User: Confirm stability?
        User->>Safe: Yes, stable
        Safe->>Safe: Clear rollback_needed.flag
        Safe-->>User: ✅ Configuration saved
    else System Fails POST (BAD SCENARIO)
        HW->>HW: POST FAILS ❌
        Note over User,HW: User must manually fix<br/>(ASUS service)
        Note over User,HW: After manual BIOS reset...
        HW->>App: Windows starts
        App->>Safe: Startup check
        Safe->>Safe: rollback_needed.flag still exists
        Safe->>Snap: Load latest snapshot
        Snap->>ACPI: Restore old values
        Safe->>Safe: Clear rollback_needed.flag
        Safe-->>User: ⚠️ Rolled back to safe config
    end
```

---

## 📊 Forbidden vs Safe Values

```mermaid
graph TD
    subgraph Forbidden["❌ FORBIDDEN - Will Prevent Boot"]
        F1["0x0000<br/>No cores enabled"]
        F2["0x0001<br/>Only 1 E-core"]
        F3["0x0100<br/>Only 1 P-core"]
        F4["0x0101<br/>Only 2 cores total"]
    end
    
    subgraph Warning["⚠️ RISKY - Use with Caution"]
        W1["0x0200<br/>2P + 0E<br/>Minimum safe"]
        W2["0x0004<br/>0P + 4E<br/>No P-cores warning"]
    end
    
    subgraph Safe["✅ SAFE - System Will Boot"]
        S1["0x0204<br/>2P + 4E<br/>Minimum recommended"]
        S2["0x0406<br/>4P + 6E<br/>Balanced"]
        S3["0x0608<br/>6P + 8E<br/>Full power<br/>(i9-13900H max)"]
        S4["0x0600<br/>6P + 0E<br/>P-cores only"]
    end
    
    Rules["VALIDATION RULES:<br/>✓ P-cores >= 2<br/>✓ Total >= 4<br/>✓ Not in forbidden list"]
    
    Rules -.->|Blocks| Forbidden
    Rules -.->|Warns| Warning
    Rules -.->|Allows| Safe
    
    style Forbidden fill:#f44336,stroke:#c62828,color:#fff
    style Warning fill:#FF9800,stroke:#E65100,color:#fff
    style Safe fill:#4CAF50,stroke:#2E7D32,color:#fff
    style Rules fill:#2196F3,stroke:#1565C0,color:#fff
```

---

## 📁 File System Structure

```mermaid
graph TD
    Root["C:\ProgramData\RamOptimizer\"]
    
    Root --> Backups["Backups\"]
    Root --> Safety["Safety\"]
    Root --> Logs["Logs\"]
    
    Backups --> F1["snapshot_factory.json<br/>🔒 NEVER DELETE"]
    Backups --> F2["snapshot_before_core.json"]
    Backups --> F3["snapshot_stable.json"]
    Backups --> F4["snapshot_latest.json<br/>Auto-updated"]
    
    Safety --> S1["rollback_needed.flag<br/>Exists if change pending"]
    Safety --> S2["boot_count.txt<br/>Tracks successful boots"]
    Safety --> S3["last_change.txt<br/>Change description"]
    
    Logs --> L1["acpi.log<br/>All ACPI operations"]
    Logs --> L2["safety.log<br/>Safety events"]
    
    style Root fill:#2196F3,stroke:#1565C0,color:#fff
    style Backups fill:#9C27B0,stroke:#6A1B9A,color:#fff
    style Safety fill:#FF9800,stroke:#E65100,color:#fff
    style Logs fill:#607D8B,stroke:#37474F,color:#fff
    style F1 fill:#4CAF50,stroke:#2E7D32,color:#fff
```

---

## 🔄 Recommended Workflow

```mermaid
stateDiagram-v2
    [*] --> TestMode: Enable Test Mode
    TestMode --> ValidateTest: Test Change (no writes)
    ValidateTest --> ReviewLogs: Review Logs
    ReviewLogs --> TestMode: Iterate if needed
    ReviewLogs --> RealMode: Looks good
    
    RealMode --> ApplyChange: Apply Change (real write)
    ApplyChange --> Reboot: Reboot System
    Reboot --> BootCheck: System Boots?
    
    BootCheck --> Verify: Yes - Verify Stability
    BootCheck --> ManualFix: No - Manual BIOS Fix
    
    Verify --> Stable: User Confirms
    Stable --> Complete: Clear Rollback Flag
    
    ManualFix --> AutoRollback: After Fix, App Detects
    AutoRollback --> Complete: Restored Safe Config
    
    Complete --> [*]
    
    note right of TestMode
        TestModeEnabled = true
        No hardware writes
        Safe testing
    end note
    
    note right of RealMode
        TestModeEnabled = false
        Real hardware writes
        Rollback protected
    end note
```

---

## 📝 How to View These Diagrams

**In VS Code:**
1. Install extension: "Markdown Preview Mermaid Support"
2. Open this file
3. Press `Ctrl+Shift+V` (or `Cmd+Shift+V` on Mac)
4. Diagrams will render beautifully!

**On GitHub:**
- Just view the file - Mermaid renders automatically

**In Browser:**
- See the HTML version being created next!

