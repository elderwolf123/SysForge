# Ram Optimizer - User Guide & Documentation Overview

## 📚 Quick Navigation

| Category | Location | Description |
|----------|----------|-------------|
| **Getting Started** | [README.md](README.md) | Project overview and setup |
| **ACPI Safety** | [docs/acpi-safety/](docs/acpi-safety/) | Critical safety system documentation |
| **Architecture** | [docs/architecture/](docs/architecture/) | System design and plugin architecture |
| **Implementation** | [docs/implementation/](docs/implementation/) | Development progress and status |
| **Testing** | [docs/testing/](docs/testing/) | Testing strategies and plans |
| **Critical Info** | [docs/critical/](docs/critical/) | Hardware recovery and current situation |
| **Archived** | [docs/deprecated/](docs/deprecated/) | Old specs and future features |

---

## 🎯 What is Ram Optimizer?

Ram Optimizer is an **ACPI safety system** for ASUS ROG laptops that prevents hardware bricking when changing CPU core configurations, battery limits, and performance modes.

### Core Features

1. **6-Layer Safety System**
   - Pre-flight validation
   - Read-after-write verification
   - Automatic snapshotting
   - Rollback protection
   - Boot failure detection
   - Test mode simulation

2. **Modular Plugin Architecture**
   - ASUS ROG plugin (full control)
   - Generic Windows plugin (monitoring only)
   - Extensible for other manufacturers

3. **G-Helper Monitoring**
   - Capture ACPI calls from G-Helper
   - Verify device IDs match
   - Ensure safety before hardware writes

---

## 📁 Documentation Organization

### `docs/acpi-safety/` - **ACPI Safety System**

The heart of the project. Everything you need to understand and use the safety features.

**Key Files:**
- `comprehensive-guide.md` - Complete ACPI safety documentation
- `diagrams.html` - Visual flow charts (open in browser)
- `bug-report.md` - Identified and fixed bugs

**What it covers:**
- How the 6-layer safety system works
- Safe vs forbidden CPU configurations
- Snapshot and rollback mechanisms
- Usage examples and integration

### `docs/architecture/` - **System Design**

Technical architecture and design decisions.

**Key Files:**
- `MODULAR_ARCHITECTURE.md` - Plugin system design ⭐ ACTIVE
- `ARCHITECTURE_DESIGN.md` - Overall system architecture

**What it covers:**
- Plugin architecture
- Interface design
- Device-agnostic core
- Extensibility for other laptops

### `docs/implementation/` - **Development Progress**

Track what's been done and what's next.

**Key Files:**
- `current-status.md` - Latest progress summary
- `DAY_1_ACTION_PLAN.md` - Initial action items
- `IMPLEMENTATION_ROADMAP.md` - Feature roadmap

**What it covers:**
- Completed phases (1-4)
- Remaining work (5-7)
- Task tracking
- Development timeline

### `docs/testing/` - **Testing & Verification**

How to test the system safely.

**Key Files:**
- `TESTING_PLAN.md` - Test strategy
- `FEATURE_VERIFICATION_CHECKLIST.md` - Feature checklist
- `HARDWARE_SAFETY_AND_TESTING_STRATEGY.md` - Safe testing approach

**What it covers:**
- Test mode usage
- Hardware testing (when laptop returns)
- Verification procedures
- Safety protocols

### `docs/critical/` - **Critical Information**

**IMPORTANT:** Hardware situation and recovery info.

**Key Files:**
- `CRITICAL_CURRENT_SITUATION.md` - Laptop bricked, at ASUS repair ⚠️
- `ROG_FLOW_Z13_BIOS_RECOVERY.md` - BIOS recovery procedures
- `ROG_FLOW_Z13_NO_NATIVE_RECOVERY.md` - Why native recovery doesn't work

**What it covers:**
- Current hardware status
- Why the laptop bricked
- ASUS service requirements
- Recovery limitations

### `docs/deprecated/` - **Archived Content**

Old specifications and future feature ideas (not currently active).

**Files archived here:**
- Old compression system specs (future feature)
- Outdated technical specifications
- Superseded designs
- Future optimization ideas

**Why it's here:**
- Not currently implemented
- May be useful later
- Historical reference
- Keeps root clean

---

## 🚀 Quick Start Guide

### For Users

1. **Read the safety guide**
   ```
   docs/acpi-safety/comprehensive-guide.md
   ```

2. **View the diagrams**
   ```
   Open: docs/acpi-safety/diagrams.html in browser
   ```

3. **Check current status**
   ```
   docs/critical/CRITICAL_CURRENT_SITUATION.md
   ```

### For Developers

1. **Understand the architecture**
   ```
   docs/architecture/MODULAR_ARCHITECTURE.md
   ```

2. **Check implementation progress**
   ```
   docs/implementation/current-status.md
   ```

3. **Run examples**
   ```csharp
   Examples/PluginUsageExample.cs  // Complete workflow
   Examples/MonitoringIntegrationExample.cs  // ACPI monitoring
   ```

---

## 🔧 Project Structure

```
Ram Optimizer/
├── src/                          # Source code
│   ├── Core/                     # Device-agnostic interfaces
│   ├── Plugins/                  # Device-specific plugins
│   │   ├── Asus/                # ASUS ROG plugin
│   │   └── Generic/             # Generic Windows plugin
│   ├── HardwareControl/         # Safety system core
│   └── Monitoring/              # ACPI monitoring
│
├── Examples/                     # Usage examples
├── docs/                        # All documentation (organized)
├── tests/                       # Unit tests
└── README.md                    # Project overview
```

---

## ⚠️ Current Status

**Hardware:** Laptop is bricked and at ASUS for repair (see `docs/critical/`)

**Software:** 
- ✅ Modular architecture complete
- ✅ All 7 bugs fixed
- ✅ ASUS & Generic plugins working
- ⏳ Waiting for hardware to test

**Next Steps:**
1. Test on real hardware (when laptop returns)
2. Create installer
3. Optimize performance
4. Release

---

## 📖 File Archive Reference

### What I'm Archiving to `docs/deprecated/`

These files are **future features** or **outdated specs** - not currently part of the active system:

**Compression Features (Future Work):**
- `ADVANCED_COMPRESSION_FEATURES_IMPLEMENTATION.md`
- `ADVANCED_TRANSPARENT_COMPRESSION_ARCHITECTURE.md`
- `REFINED_COMPRESSION_AND_BATTERY_OPTIMIZATION.md`
- `ULTRA_AGGRESSIVE_CUSTOM_COMPRESSION_SYSTEM.md`
- `SPACE_EFFICIENT_REDUNDANCY_SYSTEM.md`

**Outdated Specs:**
- `COMPREHENSIVE_TECHNICAL_IMPLEMENTATION.md` (superseded by modular architecture)
- `ULTIMATE_SYSTEM_OPTIMIZER_COMPLETE_SPECIFICATION.md` (old spec)
- `COMPREHENSIVE_SYSTEM_OPTIMIZER_ARCHITECTURE.md` (duplicate of architecture)
- `TECHNICAL_SPECIFICATION.md` (outdated)
- `ULTRA_AGGRESSIVE_SAFETY_DESIGN.md` (superseded by current safety system)
- `ABSOLUTE_FILE_INTEGRITY_PROTECTION.md` (future feature)
- `SYSTEM_STABILITY_REQUIREMENTS.md` (covered in other docs)

**Why Archive (Not Delete):**
- May implement compression features later
- Historical reference
- Design ideas to revisit
- Keeps root directory clean

None of these are currently used by the active codebase.

---

## 🆘 Need Help?

**Something broke?**
→ Check `docs/acpi-safety/bug-report.md`

**Want to understand the safety system?**
→ Read `docs/acpi-safety/comprehensive-guide.md`

**Looking for a specific feature?**
→ Check this guide's navigation table

**Want to contribute?**
→ See `docs/architecture/MODULAR_ARCHITECTURE.md` for plugin development

---

## 📝 Documentation Maintenance

**When to update:**
- User guide: When major features change
- Implementation status: After completing phases
- Bug reports: When new bugs are found/fixed
- Architecture: When design changes

**What stays in root:**
- README.md (project overview)
- This user guide (quick reference)

**Everything else:**
- Organized in `docs/` by category
