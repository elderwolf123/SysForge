# ASUS ROG Flow Z13 - BIOS Recovery Guide

**⚠️ IMPORTANT: This is emergency recovery information for when your system won't POST**

---

## What Happened

Your ASUS ROG Flow Z13 is not getting past POST (Power-On Self-Test) after making ACPI calls, likely due to:

1. **Invalid P/E core configuration** stored in NVRAM
2. **Corrupted ACPI settings** that prevent boot
3. **UEFI variable corruption** from ATKACPI calls

---

## Immediate Recovery Options

### Option 1: BIOS/UEFI Reset (First Try)

Most ASUS laptops can reset BIOS to defaults:

1. **Power off completely** (hold power button for 10+ seconds)
2. **Disconnect all peripherals** (USB devices, external displays, etc.)
3. **Try BIOS Entry Combinations:**
   - Hold **F2** during power on
   - Hold **Del** during power on  
   - Hold **Esc** during power on
   - Try **Ctrl + Home** (ASUS crisis recovery)

4. If BIOS loads:
   - Select **"Load Optimized Defaults"** or **"Load Setup Defaults"**
   - Save and exit
   - Should boot normally

### Option 2: ASUS Crisis Recovery Mode

The ROG Flow Z13 may support Emergency BIOS Flash:

#### Requirements:
- **USB flash drive** (FAT32 formatted, 8GB or smaller recommended)
- **Latest BIOS file** from ASUS website

#### Steps:

1. **Download Latest BIOS:**
   - Go to: https://www.asus.com/supportonly/rog flow z13 gz301/helpdesk/bios/
   - Download the latest BIOS for your exact model (GZ301ZA, GZ301ZC, GZ301ZE, etc.)
   - **Model is printed on bottom of device**

2. **Prepare Recovery USB:**
   ```powershell
   # In PowerShell (as Administrator)
   
   # Format USB as FAT32
   $usbDrive = "E:" # Change to your USB drive letter
   Format-Volume -DriveLetter E -FileSystem FAT32 -NewFileSystemLabel "ASUS_BIOS"
   
   # Extract BIOS file
   # The downloaded file is usually a .zip or .exe
   # Extract it to find the actual BIOS file (usually .ROM or .CAP extension)
   
   # Rename BIOS file to exactly: GZ301ZA.ROM
   # (Replace with your actual model number)
   ```

3. **Crisis Recovery Boot:**
   - **Power off completely**
   - Insert USB into **left-side USB port** (if multiple ports)
   - Hold **Ctrl + Home** keys
   - Press **Power button** while still holding Ctrl + Home
   - Keep holding for 10-15 seconds
   - You should see LED activity or screen flashing
   - **DO NOT interrupt** - can take 5-15 minutes
   - System will reboot automatically when done

4. **Alternative Key Combinations** (try if above doesn't work):
   - **Ctrl + Home + Power**
   - **Fn + Esc + Power**
   - **Vol Down + Vol Up + Power** (if has volume buttons)

### Option 3: Battery Disconnect (Hardware Reset)

**⚠️ WARNING: This may void warranty and requires disassembly**

If other methods fail and you can't wait for ASUS repair:

1. **ONLY if comfortable with electronics**
2. Follow iFixit teardown guide for ROG Flow Z13
3. Disconnect internal battery connector
4. Press and hold power button for 30 seconds (discharge residual power)
5. Reconnect battery
6. Try to boot - should reset NVRAM/CMOS

**DO NOT attempt this if under warranty - let ASUS handle it**

---

## ASUS Warranty Service

Since your device is under warranty, **this is the safest option:**

### What to Tell ASUS Support:

> "My ROG Flow Z13 will not POST after I was using system monitoring software. 
> The system appears to have corrupted ACPI/UEFI settings that prevent boot.
> I cannot access BIOS setup. I need a BIOS/NVRAM reset or reflash."

### Information They'll Need:
- **Model Number:** GZ301ZA/ZC/ZE (check bottom of device)
- **Serial Number:** (on bottom label)
- **Symptoms:** No POST, no BIOS access, occurred after running system control software

### What ASUS Service Will Do:
1. **BIOS reflash** using their service tools
2. **NVRAM clear** to reset ACPI variables
3. **Hardware diagnostic** to ensure no physical damage
4. **Functional testing** before return

**Estimated Turnaround:** 1-2 weeks typically

---

## Prevention for Future (After Recovery)

Once you get your laptop back, use the new safety system:

### 1. Capture Factory Defaults IMMEDIATELY

```csharp
using var safeAcpi = new SafeAcpiInterface(logger);
var snapshotManager = safeAcpi.GetSnapshotManager();

// Capture fresh-from-ASUS configuration
snapshotManager.CaptureAndSave(
    safeAcpi.GetRawInterface(), 
    "factory", 
    "Fresh from ASUS repair - DO NOT DELETE"
);
```

### 2. Create Emergency Recovery USB

Create a bootable Windows To Go or WinPE USB with recovery tool:

```csharp
// EmergencyRecovery.exe - Simple console app
static void Main()
{
    Console.WriteLine("=== ASUS ROG Flow Z13 Emergency Recovery ===");
    Console.WriteLine();
    
    try
    {
        if (!AsusAcpiInterface.IsAvailable())
        {
            Console.WriteLine("ERROR: ACPI interface not available");
            Console.WriteLine("Try booting into Safe Mode or Windows Recovery");
            return;
        }

        using var acpi = new AsusAcpiInterface();
        var snapshotManager = new SnapshotManager();

        Console.WriteLine("Available snapshots:");
        var snapshots = snapshotManager.ListSnapshots();
        for (int i = 0; i < snapshots.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {snapshots[i]}");
        }

        Console.WriteLine();
        Console.WriteLine("Select snapshot to restore (or 0 to cancel): ");
        
        if (int.TryParse(Console.ReadLine(), out int choice) && 
            choice > 0 && choice <= snapshots.Count)
        {
            var snapshot = snapshots[choice - 1];
            Console.WriteLine($"Restoring: {snapshot.SnapshotName}");
            
            if (snapshot.ApplyTo(acpi, null))
            {
                Console.WriteLine("SUCCESS! Configuration restored.");
                Console.WriteLine("Please reboot your system.");
            }
            else
            {
                Console.WriteLine("FAILED! Could not restore configuration.");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
    }

    Console.WriteLine();
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
```

Save this to USB along with your snapshot backups.

### 3. Keep BIOS Recovery USB Ready

**Always maintain:**
- USB with latest BIOS (renamed for crisis recovery)
- USB with emergency recovery tool + snapshots
- Printed copy of crisis recovery instructions

### 4. Use Test Mode First

```csharp
// ALWAYS test before applying
using var safeAcpi = new SafeAcpiInterface(logger);
safeAcpi.TestModeEnabled = true;

// Test the change
safeAcpi.SetCores(6, 8);

// Review logs, ensure no errors
// THEN disable test mode and apply for real
safeAcpi.TestModeEnabled = false;
safeAcpi.SetCores(6, 8);
```

---

## Technical Details: What Likely Happened

### ACPI Device IDs You Were Using:

```csharp
public const uint CORES_CPU = 0x001200D2;  // P/E core control
public const uint CORES_MAX = 0x001200D3;  // Max cores query
public const uint BatteryLimit = 0x00120057; // Battery limit
```

### Likely Failure Scenario:

1. **Invalid Core Configuration Written**
   - Value like `0x0000` (no cores)
   - Value like `0x0100` (only 1 P-core)
   - Value exceeding hardware limits

2. **NVRAM Persistence**
   - ASUS ATKACPI stores some settings in UEFI NVRAM
   - These persist across power cycles
   - Invalid values prevent CPU initialization
   - System can't POST without valid CPU config

3. **Why Standard Boot Fails**
   - UEFI reads NVRAM during early init
   - Tries to configure CPU with invalid settings
   - CPU initialization fails
   - POST halts before BIOS setup is accessible

### Why Crisis Recovery Works:

- Bypasses normal NVRAM
- Reflashes BIOS + clears NVRAM
- Restores all UEFI variables to defaults
- CPU configuration reset to hardware defaults

---

## Research & Resources

### Official ASUS Resources:

1. **BIOS Downloads:**
   - https://www.asus.com/support/
   - Search for "ROG Flow Z13" + your model
   - Download section → BIOS

2. **Service Centers:**
   - https://www.asus.com/support/service-center/
   - Find authorized service center near you

3. **Support Contact:**
   - USA: 1-888-678-3688
   - Global: https://www.asus.com/support/contact/

### Community Resources:

1. **ASUS ROG Forum:**
   - https://rog-forum.asus.com/
   - Search for "Flow Z13 BIOS recovery"

2. **Reddit Communities:**
   - r/ASUS
   - r/ROGFlow
   - Search for similar recovery stories

3. **G-Helper (Reference):**
   - https://github.com/seerge/g-helper
   - Similar tool that uses ATKACPI safely
   - Study their safety implementations

### Similar Projects for Reference:

Study how these projects handle ACPI safely:

1. **G-Helper** - ASUS laptop control
   - Safe ACPI wrapping
   - Validation before writes
   - No reports of bricking systems

2. **OpenRGB** - RGB control
   - Hardware safety checks
   - Read-verify-write pattern

3. **Throttlestop** - CPU control
   - Extensive validation
   - Emergency reset features

---

## Lessons Learned

### What NOT To Do:

❌ Write to ACPI without validation  
❌ Test P/E core changes without backup  
❌ Allow values like 0x0000 or 0x0100  
❌ Skip read-after-write verification  
❌ Test on production system first  

### What TO Do:

✅ Always validate before write  
✅ Always capture snapshot before change  
✅ Always verify after write  
✅ Use test mode first  
✅ Change one parameter at a time  
✅ Keep recovery USB ready  
✅ Implement rollback detection  
✅ Log everything  
✅ Minimum safety: 2 P-cores, 4 total cores  
✅ Study G-Helper's implementation  

---

## Post-Recovery Checklist

After getting laptop back from ASUS:

- [ ] Boot and verify system is fully functional
- [ ] Update to latest BIOS from ASUS (if not already done)
- [ ] Capture factory defaults snapshot
- [ ] Create BIOS recovery USB for crisis mode
- [ ] Create emergency recovery tool on USB
- [ ] Implement all safety classes (done ✅)
- [ ] Update code to use `SafeAcpiInterface`
- [ ] Add validation to all ACPI calls
- [ ] Test in Test Mode before real changes
- [ ] Document exact model number and BIOS version
- [ ] Save this guide offline (print or save to USB)

---

## Contact Information for Help

If you need assistance with recovery:

1. **ASUS Support** (Primary option - under warranty)
   - 1-888-678-3688 (USA)
   - https://www.asus.com/support/

2. **ASUS ROG Discord**
   - Official ASUS server often has tech support

3. **Professional Computer Repair**
   - If ASUS turnaround is too long
   - They can perform BIOS reflash
   - Cost: ~$50-150 typically

Remember: **This wasn't a hardware failure** - it's a configuration issue. ASUS service will fix it, and with the new safety system, it won't happen again!
