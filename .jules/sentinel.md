## 2024-05-24 - [Path Hijacking in GpuOptimizers]
**Vulnerability:** Path Hijacking (Untrusted Search Path) / Local Privilege Escalation (LPE) in `GpuOptimizer.cs`, `AdvancedGpuOptimizer.cs`, and `GpuResourceOptimizer.cs`.
**Learning:** Terminating processes and saving only their raw `processName` in a persistent state file (`gpu_optimizer_state.json`), then passing this raw name directly to `Process.Start` during recovery allows an attacker to execute an arbitrary executable if placed earlier in the system's `PATH`.
**Prevention:** To prevent Path Hijacking when tracking and restarting processes, capture and store the absolute path (`MainModule.FileName`) of the process at termination time, handling potential `Win32Exception` (Access Denied) gracefully.
