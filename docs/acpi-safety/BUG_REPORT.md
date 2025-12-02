# Bug Report & Fixes

## 🐛 Bugs Found in Safety Code

### **Bug #1: Race Condition in Battery Limit Clear Rollback Flag**
**File:** `SafeAcpiInterface.cs:159`

**Issue:**
```csharp
Task.Delay(5000).ContinueWith(_ => _rollback.ClearRollbackFlag());
```

**Problem:**  
- Uses fire-and-forget Task without awaiting
- Doesn't handle exceptions
- If object is disposed before 5 seconds, will throw
- Flag might not clear if exception occurs

**Severity:** HIGH - Could leave rollback flag set incorrectly

**Fix:**
```csharp
// Option 1: Remove auto-clear for battery (user should confirm)
// _rollback.ClearRollbackFlag();  // Remove this line

// Option 2: Use safe background task
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(5000);
        if (!_disposed)
        {
            _rollback.ClearRollbackFlag();
        }
    }
    catch (Exception ex)
    {
        _logger?.LogError($\"Failed to clear rollback flag: {ex.Message}\");
    }
});
```

---

### **Bug #2: Missing Verification for Performance Mode**
**File:** `SafeAcpiInterface.cs:196-200`

**Issue:**
```csharp
_logger?.LogInformation($"Setting performance mode to {mode}");
_acpi.DeviceSet(AsusAcpiInterface.PerformanceMode, mode);
return true;
```

**Problem:**
- No read-after-write verification
- Doesn't check if DeviceSet succeeded
- Assumes write worked

**Severity:** MEDIUM - Could silently fail

**Fix:**
```csharp
_logger?.LogInformation($"Setting performance mode to {mode}");
int result = _acpi.DeviceSet(AsusAcpiInterface.PerformanceMode, mode);

if (result != 1)
{
    _logger?.LogError($"Failed to set performance mode. ACPI returned: {result}");
    return false;
}

// Verify
System.Threading.Thread.Sleep(100);
int currentMode = _acpi.DeviceGet(AsusAcpiInterface.PerformanceMode);
if (currentMode != mode)
{
    _logger?.LogWarning($"Performance mode verification failed. Expected {mode}, got {currentMode}");
}

return true;
```

---

### **Bug #3: Missing Null Check in Rollback**
**File:** `SafeModeRollback.cs:122`

**Issue:**
```csharp
var snapshot = snapshotManager.LoadLatestSnapshot();
if (snapshot != null)
{
    _logger?.LogWarning($"Restoring snapshot: {snapshot}");
    bool success = snapshot.ApplyTo(acpi, _logger);
```

**Problem:**
- `snapshot.ApplyTo()` might be called with null `acpi`
- No validation that `acpi` parameter is not null
- Could throw NullReferenceException

**Severity:** MEDIUM - Could crash during rollback

**Fix:**
```csharp
public bool CheckAndRollback(AsusAcpiInterface? acpi, SnapshotManager? snapshotManager)
{
    if (acpi == null || snapshotManager == null)
    {
        _logger?.LogError("Cannot perform rollback: null dependencies");
        return false;
    }
    
    // ... rest of method
}
```

---

### **Bug #4: Hard-Coded Sleep Values**
**Files:** Multiple

**Issue:**
```csharp
System.Threading.Thread.Sleep(100);  // Hard-coded delays
```

**Problem:**
- Magic numbers throughout code
- No constants defined
- Blocks thread (not async)
- Might be too short or too long depending on system

**Severity:** LOW - Works but not ideal

**Fix:**
```csharp
// In a constants file
public static class AcpiConstants
{
    public const int ACPI_WRITE_DELAY_MS = 100;
    public const int ACPI_VERIFY_DELAY_MS = 200;
    public const int BATTERY_CONFIRM_DELAY_MS = 5000;
}

// Usage
await Task.Delay(AcpiConstants.ACPI_VERIFY_DELAY_MS);
```

---

### **Bug #5: Snapshot Order Issue in SetCores**
**File:** `SafeAcpiInterface.cs:59-62`

**Issue:**
```csharp
_snapshotManager.CaptureAndSave(_acpi, "before_core_change", description);
_rollback.SetRollbackFlag(description);
```

**Problem:**
- Captures snapshot  
- THEN sets rollback flag
- If program crashes between these, snapshot exists but no rollback flag
- On next boot, wrong snapshot might be used

**Severity:** LOW - Edge case but possible

**Fix:**
```csharp
// Set rollback flag FIRST
_rollback.SetRollbackFlag(description);

// THEN capture snapshot
_logger?.LogInformation($"Capturing snapshot before change: {description}");
_snapshotManager.CaptureAndSave(_acpi, "before_core_change", description);
```

---

### **Bug #6: Missing Disposal Check**
**File:** `SafeAcpiInterface.cs` - All methods

**Issue:**
```csharp
public bool SetCores(...)
{
    // No check if already disposed
    _acpi.DeviceSet(...);
}
```

**Problem:**
- Methods can be called after Dispose()
- Will throw ObjectDisposedException
- No guard clauses

**Severity:** MEDIUM - Could crash if misused

**Fix:**
```csharp
public bool SetCores(int pCores, int eCores, string? changeDescription = null)
{
    if (_disposed)
    {
        throw new ObjectDisposedException(nameof(SafeAcpiInterface));
    }
    
    // ... rest of method
}
```

---

### **Bug #7: TestMode Bypass Still Sets Rollback Flag**
**File:** `SafeAcpiInterface.cs:62-68`

**Issue:**
```csharp
_rollback.SetRollbackFlag(description);  // Line 62

if (TestModeEnabled)  // Line 64
{
    _logger?.LogWarning($"[TEST MODE] Would set cores...");
    return true;  // Returns without clearing flag!
}
```

**Problem:**
- Sets rollback flag
- If in test mode, returns early
- Flag is NEVER cleared
- On next boot, unnecessary rollback triggered

**Severity:** HIGH - Breaks test mode

**Fix:**
```csharp
// Don't set rollback flag in test mode
if (!TestModeEnabled)
{
    _rollback.SetRollbackFlag(description);
}

if (TestModeEnabled)
{
    _logger?.LogWarning($"[TEST MODE] Would set cores to P={pCores}, E={eCores}");
    return true;
}
```

---

## 📊 Summary

| Bug # | Severity | File | Issue |
|-------|----------|------|-------|
| #1 | HIGH | SafeAcpiInterface.cs | Race condition in battery rollback clear |
| #2 | MEDIUM | SafeAcpiInterface.cs | Missing perf mode verification |
| #3 | MEDIUM | SafeModeRollback.cs | Missing null checks |
| #4 | LOW | Multiple | Hard-coded sleep values |
| #5 | LOW | SafeAcpiInterface.cs | Snapshot order issue |
| #6 | MEDIUM | SafeAcpiInterface.cs | Missing disposal checks |
| #7 | HIGH | SafeAcpiInterface.cs | Test mode sets rollback flag |

##  Priority Fixes
1. **#7** - Test mode rollback flag (critical for testing)
2. **#1** - Battery rollback race condition
3. **#6** - Disposal checks
4. **#3** - Null checks in rollback
5. **#2** - Performance mode verification
6. **#5** - Snapshot ordering
7. **#4** - Magic number constants

---

**All bugs identified. Ready to apply fixes!** 🔧
