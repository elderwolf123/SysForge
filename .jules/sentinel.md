## 2024-05-09 - [CRITICAL] Prevent Command Injection in Process Executions
**Vulnerability:** Found insecure usage of `Process.Start(string)` and `ProcessStartInfo` where processes were launched without explicit restrictions, notably leaving `UseShellExecute` as default or `true`, which creates severe command injection and execution control risks.
**Learning:** `Process.Start` without enforcing `UseShellExecute = false` allows commands to be interpreted by the operating system shell. In contexts where process names or paths can be manipulated or aren't strictly hardcoded, this poses a risk of executing arbitrary commands.
**Prevention:** Always use `ProcessStartInfo` to launch processes and explicitly set `UseShellExecute = false` and `CreateNoWindow = true` (unless shell execution or window visibility are absolutely necessary and input is strictly sanitized).
## 2024-05-14 - [CRITICAL] Prevent Path Hijacking in System Utility Execution
**Vulnerability:** Found insecure usage of relative paths / bare executable names (e.g., `sc.exe`, `wmic`, `powercfg`, `explorer.exe`) in `ProcessStartInfo` to launch system utilities.
**Learning:** Using relative paths allows malicious actors to place a rogue executable with the same name in a directory that occurs earlier in the system's `PATH` environment variable, leading to Local Privilege Escalation (LPE) or unintended code execution, especially when the process is executed with elevated privileges.
**Prevention:** Always use absolute, fully-qualified paths constructed securely using `Environment.GetFolderPath(Environment.SpecialFolder.System)` (or `.Windows`) combined with `Path.Combine` when starting system utilities via `Process.Start`.
## 2024-05-18 - [CRITICAL] Prevent Path Hijacking in Process Recovery
**Vulnerability:** Process recovery logic stored relative executable names (e.g., "nvidia-smi.exe") and later restarted them via `Process.Start(name)`, introducing Path Hijacking vulnerabilities if the system PATH changes.
**Learning:** We must capture `process.MainModule.FileName` *before* calling `process.Kill()` to safely store the absolute path, as a terminated process will throw an exception when accessing `MainModule`.
**Prevention:** Always capture and persist the absolute executable path before terminating a process if it needs to be securely restarted later.
