# Day 1: When Laptop Returns From ASUS

**Goal:** Safely validate the system and capture baseline before ANY changes

---

## ⚠️ CRITICAL: Do These IN ORDER

### Step 1: Capture Factory Snapshot (FIRST PRIORITY)
**Time: 2 minutes | Risk: ZERO**

```csharp
using var safeAcpi = new SafeAcpiInterface(logger);

// IMMEDIATELY capture factory state - before ANYTHING else
safeAcpi.GetSnapshotManager().CaptureAndSave(
    safeAcpi.GetRawInterface(),
    "factory_asus_repair",
    "CAPTURED AFTER ASUS REPAIR - NEVER DELETE - EMERGENCY ONLY"
);
```

**Then IMMEDIATELY:**
1. Copy `factory_asus_repair.json` to USB drive
2. Email it to yourself
3. Upload to Google Drive / OneDrive
4. Keep original in `C:\ProgramData\RamOptimizer\Backups\`

**This is your insurance policy. Do NOT skip this.**

---

### Step 2: Install Monitoring Tools (Optional but Recommended)
**Time: 10 minutes | Risk: ZERO**

1. **Download Procmon:**
   - https://learn.microsoft.com/en-us/sysinternals/downloads/procmon
   - Extract to `C:\Tools\Procmon\`

2. **Install G-Helper:**
   - https://github.com/seerge/g-helper/releases
   - Already tested by thousands, proven safe
   - Will use for validation monitoring

**DON'T USE YET - just install**

---

### Step 3: Mandatory Read-Only Validation
**Time: 5 minutes | Risk: ZERO (only reads)**

```csharp
var validation = new MandatoryValidationWrapper(logger);

// This ONLY reads values, never writes
bool passed = validation.RunMandatoryReadOnlyTests();

if (passed)
{
    Console.WriteLine("✅ Device IDs verified - safe to proceed");
}
else
{
    Console.WriteLine("❌ STOP - Device IDs may be wrong");
    Console.WriteLine("Review logs and compare with G-Helper");
}
```

**What this does:**
- Reads `CORES_MAX` → Should get `0x0608` (6P+8E for i9-13900H)
- Reads `CORES_CPU` → Should get current config
- Reads `BatteryLimit` → Should get 60-100%

**If values are garbage → STOP, investigate**

---

### Step 4: Monitor G-Helper (Recommended)
**Time: 15 minutes | Risk: ZERO**

**Purpose:** Verify our device IDs match G-Helper exactly

1. **Start Procmon as Admin**
2. **Set filters:**
   - Process Name: `GHelper.exe`
   - Operation: `DeviceIoControl`
   - Path contains: `ATKACPI`
3. **Open G-Helper**
4. **Change cores:** 6P+8E → 4P+6E
5. **Watch Procmon capture:**
   ```
   DeviceSet(0x001200D2, 0x0406)
   ```
6. **Verify:** Our `CORES_CPU = 0x001200D2` ✅

**Result:** Confirms device IDs are correct

---

### Step 5: Week 1 - Test Mode ONLY
**Time: Ongoing | Risk: ZERO (no hardware writes)**

```csharp
// FOR THE ENTIRE FIRST WEEK
using var safeAcpi = new SafeAcpiInterface(logger);

#if DEBUG
    safeAcpi.TestModeEnabled = true;  // FORCE test mode
#endif

// Test all your logic
safeAcpi.SetCores(4, 6);
safeAcpi.SetBatteryLimit(80);
// etc.

// Review logs - NO actual hardware writes
// Build confidence in validation logic
```

**No actual ACPI writes for at least 1 week of testing**

---

### Step 6: First Real Write (Week 2+)
**Time: 5 minutes | Risk: LOW (writing current value)**

**Safest possible first real write:**

```csharp
// After 1+ week of test mode, ready for first real write
safeAcpi.TestModeEnabled = false;

// Get current values
var coreManager = new CoreManager(safeAcpi.GetRawInterface());
var (currentP, currentE) = coreManager.GetCurrentCores();

// Write THE SAME value that's already set (safest possible)
Console.WriteLine($"First real write: {currentP}P + {currentE}E (current value)");
bool success = safeAcpi.SetCores(currentP, currentE);

if (success)
{
    Console.WriteLine("✅ Write successful and verified!");
    Console.WriteLine("Device IDs are CONFIRMED correct");
}
else
{
    Console.WriteLine("❌ Write failed - investigate");
}
```

**Why this is safe:**
- Writing the value that's ALREADY set
- Can't break anything (already in this state)
- Confirms write/verify logic works
- Validates device IDs are correct

---

### Step 7: Gradual Rollout (Week 3+)
**Time: Gradual over weeks**

Only after Step 6 succeeds:

**Week 3:** Change battery limit (safe, no reboot)
```csharp
safeAcpi.SetBatteryLimit(80);
// Wait 1 week, observe
```

**Week 4:** Change to MAXIMUM cores (safest core change)
```csharp
safeAcpi.SetCores(6, 8);  // Maximum - proven safe
// Reboot, confirm stable
// Wait 1 week
```

**Week 5+:** Try other configurations if needed
```csharp
safeAcpi.SetCores(4, 6);  // Lower cores
// Reboot, confirm stable
```

**ONE CHANGE AT A TIME with multi-day gaps**

---

## ❌ What NOT To Do

**DON'T:**
- ❌ Skip capturing factory snapshot
- ❌ Make ANY changes Day 1
- ❌ Use real mode before test mode
- ❌ Write new values before writing current value
- ❌ Make multiple changes at once
- ❌ Use without validation passing
- ❌ Bypass safety checks "just this one time"

**DO:**
- ✅ Capture factory snapshot IMMEDIATELY
- ✅ Back up snapshot to multiple places
- ✅ Run mandatory read-only tests
- ✅ Monitor G-Helper to verify device IDs
- ✅ Use test mode for at least 1 week
- ✅ First real write = current value
- ✅ One change at a time with waiting periods

---

## 📊 Risk Assessment by Activity

| Activity | Risk Level | When To Do |
|----------|-----------|------------|
| Capture factory snapshot | **ZERO** | Day 1 - IMMEDIATELY |
| Read-only validation | **ZERO** | Day 1 - Required |
| Monitor G-Helper | **ZERO** | Day 1-7 - Recommended |
| Test mode (1 week) | **ZERO** | Week 1 - Required |
| Write current value | **LOW** | Week 2+ - After tests pass |
| Change battery limit | **LOW** | Week 3+ - After first write works |
| Set max cores (6P+8E) | **MEDIUM** | Week 4+ - Proven safe value |
| Set reduced cores | **MEDIUM** | Week 5+ - After max cores stable |

---

## 📋 Pre-Flight Checklist

Before making ANY real hardware change:

- [ ] Factory snapshot captured and backed up to 3+ places
- [ ] Mandatory read-only tests passed
- [ ] G-Helper monitoring confirms device IDs match
- [ ] Test mode used for 1+ week, reviewed logs
- [ ] First write (current value) succeeded
- [ ] Current snapshot captured
- [ ] Rollback flag system tested in test mode
- [ ] Validation system confirmed working
- [ ] User understands rollback procedure
- [ ] Ready to reboot and confirm stability

**ALL boxes must be checked before proceeding**

---

## 🆘 If Something Goes Wrong

### Scenario 1: Read-Only Tests Fail
**Symptom:** Read values are garbage or unexpected

**Action:**
1. DON'T write anything
2. Capture logs and values
3. Compare device IDs with G-Helper monitoring
4. Ask for help with analysis
5. DO NOT PROCEED until resolved

### Scenario 2: First Write Fails Verification
**Symptom:** Write appears successful, but read-back doesn't match

**Action:**
1. System auto-rolls back (if SafeAcpiInterface working)
2. Review logs for error details
3. Verify device IDs with G-Helper
4. DO NOT retry until root cause identified

### Scenario 3: System Unstable After Change
**Symptom:** Crashes, freezes, or instability after reboot

**Action:**
1. Don't confirm stable
2. SafeAcpiInterface auto-restores snapshot
3. System returns to previous state
4. Review what went wrong
5. Adjust and try again later (or don't)

### Scenario 4: POST Failure (Worst Case)
**Symptom:** System won't show BIOS screen

**Action:**
1. Back to ASUS for BIOS reflash (warranty)
2. This is WHY validation is critical
3. If validation passed, this shouldn't happen
4. If it does, investigate validation logic

---

## 💡 Bottom Line

**Day 1 Priority:**
1. Capture factory snapshot
2. Back it up to 3+ places
3. Run read-only validation
4. Install monitoring tools

**Then WAIT and TEST for 1-2 weeks before first real write**

**Be patient. There's no rush. The safety system is built. Now just validate it thoroughly.**

---

## 📞 Contact Points

If you need help:
1. Review logs in `C:\ProgramData\RamOptimizer\Logs\`
2. Check validation results
3. Compare with G-Helper behavior
4. Review the safety guides created
5. Ask questions before proceeding if unsure

**When in doubt, DON'T proceed. Test mode is always safe.**
