# Console App Enhancement Notes

## File Corruption Issue
`RamOptimizerConsole/Program.cs` was corrupted during last edit - methods inserted into menu display code.

### Quick Fix Needed:
Rebuild Program.cs clean structure or restore from before corruption at line 117.

## Missing Features to Add

### Hardware Optimization Menus (Requested):
1. **CPU Optimization**
   - Test CPU optimization module
   - Execute CPU affinity/priority adjustments

2. **GPU Optimization** 
   - Test GPU resource optimization
   - Execute GPU priority allocation

3. **Fan Control**
   - Display current fan speeds
   - Adjust fan curves (if supported)

4. **Power/Performance Settings**
   - Test performance mode switching
   - Apply power profiles
   - Battery optimization modes

5. **I/O Optimization** (partially added)
   - Test I/O priority module
   - Execute I/O priority boost

### Menu Structure Should Be:
```
MODULE TESTING:
1. RAM Optimization
2. Hardware Control (BIOS)
3. File Compression
4. CPU Optimization [ADD]
5. GPU Optimization [ADD]
6. I/O Priority [ADD]
7. Fan Control [ADD]
8. Power Profiles [ADD]
9. Test All Modules

VALIDATION TOOLS:
10. Process Blacklist
11. Compression Safety
12. Hardware Safety

OPTIMIZATION (LIVE):
13. Execute RAM
14. Execute Compression
15. Execute I/O
16. Execute CPU [ADD]
17. Execute GPU [ADD]
18. Adjust Hardware (BIOS)
19. Set Performance Mode [ADD]

SETTINGS:
20. Toggle DryRun/LIVE
21. System Info
22. View Logs
```

## Estimated Effort:
- Fix Program.cs corruption: 15 minutes
- Add missing optimizers: 30 minutes
- Test and rebuild: 15 minutes
**Total: ~1 hour**

Recommend: Fresh task/session to add these cleanly