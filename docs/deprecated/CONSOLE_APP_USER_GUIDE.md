# RAM Optimizer NOVA - Console Edition User Guide

## 🚀 Quick Start

### Location
The executable is located at:
```
./Release/Console/RamOptimizerNova.exe
```

### Run the Application
1. Navigate to the Release/Console folder
2. Run `RamOptimizerNova.exe`
3. **The application starts in DRY RUN mode for safety**

---

## 🛡️ Safety Features

### DRY RUN Mode (Default)
- **ALL operations are simulated**
- **NO actual system changes are made**
- Perfect for testing and validation
- Shows exactly what would happen

### LIVE Mode (Optional)
- Requires explicit user confirmation
- Applies real system changes
- Use only after thorough testing in DRY RUN

---

## 📋 Main Menu Options

### 🧪 MODULE TESTING (Options 1-4)

#### 1. Test RAM Optimization
**What it does:**
- Analyzes all running processes
- Shows what would be terminated at each level (1-7)
- Displays potential memory savings
- Validates that critical processes are protected

**Use this to:**
- Preview which processes would be killed
- Find the optimal aggression level for your system
- Verify system safety before executing

#### 2. Test Hardware Control (ASUS BIOS)
**What it does:**
- Tests ASUS ACPI interface availability
- Shows current hardware configuration (cores, battery, temps)
- Tests DryRun operations (no real changes)
- Validates BIOS protection is active

**Use this to:**
- Verify hardware control works on your ASUS device
- Check current system configuration
- Test changes safely before applying

#### 3. Test File Compression
**What it does:**
- Creates test files
- Tests compression/decompression integrity
- Validates data safety
- Checks various file types

**Use this to:**
- Verify compression works correctly
- Ensure no data loss will occur
- Test different file types

#### 4. Test All Modules
**What it does:**
- Runs all module tests sequentially
- Provides comprehensive system health check
- Reports pass/fail for each module

**Use this to:**
- Quick overall system validation
- Verify all components are operational

### ✅ VALIDATION TOOLS (Options 5-7)

#### 5. Validate Process Blacklist Coverage
**What it does:**
- Lists all critical processes that MUST be protected
- Shows which critical processes are currently running
- Previews termination by level (shows what would be killed)
- Verifies safety mechanisms are working

**Use this to:**
- **Ensure core system processes are protected**
- Check that Windows won't break
- Find processes that shouldn't be killed
- Verify the blacklist is comprehensive

**CRITICAL**: Run this before any RAM optimization!

#### 6. Verify Compression Safety
**What it does:**
- Creates test data and compresses it
- Verifies decompression restores original
- Tests checksum/hash validation
- Ensures data integrity

**Use this to:**
- Verify no data will be lost
- Check compression algorithms work
- Test corruption detection

#### 7. Check Hardware Safety (ASUS BIOS Protection)
**What it does:**
- Tests all safety validators
- Verifies forbidden configurations are blocked
- Tests snapshot/rollback system
- Confirms BIOS protection is active

**Use this to:**
- **Ensure BIOS won't be corrupted**
- Verify ROG Flow Z13 protection works
- Test recovery mechanisms

### ⚡ OPTIMIZATION (Options 8-10) - LIVE Mode Only

**These options require switching to LIVE mode first (Option 11)**

#### 8. RAM Optimization - Execute Level 1-7
- Prompts for aggression level (1-7)  
- Requires CONFIRM to proceed
- Actually terminates processes
- **USE WITH CAUTION**

#### 9. File Compression - Compress Folder
- Prompts for folder path
- Compresses files in folder
- Requires LIVE mode

#### 10. Hardware Control - Adjust ASUS Settings
- Submen

u for ASUS ROG hardware
- Set core configuration
- Set battery limit
- Set performance mode
- **REQUIRES REBOOT FOR CORE CHANGES**

### ⚙️ SETTINGS (Options 11-13)

#### 11. Toggle Mode
**Switches between DRY RUN and LIVE mode**

From DRY RUN → LIVE:
```
⚠️  WARNING: Real system changes will be applied!
Use with caution!
```

From LIVE → DRY RUN:
```
✅ Switched to DRY RUN Mode
All operations will be simulated - NO real changes
```

#### 12. View System Information
Shows:
- Operating system details
- Processor count
- Total RAM
- ASUS hardware availability
- Current hardware configuration
- Current mode (DRY RUN/LIVE)

#### 13. View Logs
Shows recent log files with:
- File names
- Modification dates
- File sizes

---

## 🎯 Recommended Usage Workflow

### First Time Setup:
1. **Run Option 5: Validate Process Blacklist** ✅
   - Verify critical processes are protected
   - Check coverage is complete
   
2. **Run Option 7: Check Hardware Safety** ✅
   - Verify ASUS BIOS protection is active
   - Check snapshot system works

3. **Run Option 6: Verify Compression Safety** ✅
   - Ensure no data loss risk

### Before Each RAM Optimization:
1. **Select Option 1: Test RAM Optimization** 🧪
   - Preview what would be terminated
   - Choose appropriate aggression level
   
2. **Verify Mode is DRY RUN** 🛡️
   - Check top of screen shows "DRY RUN"
   
3. **Review Preview Results** 📊
   - Ensure no critical processes would be killed
   - Check memory savings are acceptable

### When Ready to Execute:
1. **Switch to LIVE Mode** (Option 11) ⚡
2. **Execute RAM Optimization** (Option 8)
3. **Type CONFIRM when prompted**
4. **Monitor results**
5. **Switch back to DRY RUN** (Option 11) 🛡️

### For ASUS Hardware Changes:
1. **ALWAYS test in DRY RUN first** (Option 2)
2. **Create snapshot before changes** (automatic)
3. **Switch to LIVE Mode**
4. **Make ONE change at a time**
5. **Reboot if changing cores**
6. **Verify system stability**
7. **Confirm stable or rollback**

---

## ⚠️ Important Warnings

### DO NOT:
- ❌ Switch to LIVE mode without testing first
- ❌ Execute Level 7 RAM optimization unless desperate
- ❌ Change ASUS core configuration without understanding
- ❌ Ignore validation warnings

### ALWAYS:
- ✅ Start in DRY RUN mode
- ✅ Test before executing
- ✅ Verify blacklist coverage
- ✅ Read preview results
- ✅ Have BIOS recovery USB ready (for ASUS hardware changes)

---

## 🐛 Troubleshooting

### "Hardware Control Not Available"
- **Normal on non-ASUS devices**
- Other features still work
- Only affects ASUS-specific controls

### "Cannot execute in DRY RUN mode"
- Switch to LIVE mode first (Option 11)
- Confirm you want to make real changes
- Use Testing menu for safe previews

### Logs Location
```
C:\ProgramData\RamOptimizer\Logs\
```

---

## 📊 Understanding RAM Optimization Levels

| Level | Description | Risk | Memory Freed |
|-------|-------------|------|--------------|
| **1** | User Applications | ✅ Low | Low |
| **2** | Background Services | ✅ Low | Medium |
| **3** | System Utilities | ⚠️ Medium | Medium-High |
| **4** | Optional Services | ⚠️ Medium | High |
| **5** | Shell Components | ❌ High | Very High |
| **6** | Background Processes | ❌ High | Very High |
| **7** | Ultra Aggressive | 🔴 Extreme | Maximum |

**Recommendation**: Start with Level 1 or 2, increase only if needed.

---

## 🔧 Advanced Features

### Snapshot/Rollback System
- Automatic snapshots before hardware changes
- Rollback on system instability
- Manual rollback available
- Located: `C:\ProgramData\RamOptimizer\Backups\`

### Compression System
- Transparent compression support
- Multiple compression algorithms
- Integrity verification
- Automatic decompression

---

## 💡 Tips for Best Results

1. **Use DRY RUN extensively** - Test everything first
2. **Start conservative** - Use lower aggression levels initially
3. **Monitor results** - Watch memory freed vs processes killed
4. **Document changes** - Note what works for your system
5. **Keep logs** - Helpful for troubleshooting

---

## 📞 Support

- Check logs in `C:\ProgramData\RamOptimizer\Logs\`
- Review validation results before LIVE mode
- Test individual modules if issues arise
- Use DRY RUN mode to diagnose problems

---

**Remember: Safety first! When in doubt, stay in DRY RUN mode.**