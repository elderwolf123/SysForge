## 2024-05-09 - [CRITICAL] Prevent Command Injection in Process Executions
**Vulnerability:** Found insecure usage of `Process.Start(string)` and `ProcessStartInfo` where processes were launched without explicit restrictions, notably leaving `UseShellExecute` as default or `true`, which creates severe command injection and execution control risks.
**Learning:** `Process.Start` without enforcing `UseShellExecute = false` allows commands to be interpreted by the operating system shell. In contexts where process names or paths can be manipulated or aren't strictly hardcoded, this poses a risk of executing arbitrary commands.
**Prevention:** Always use `ProcessStartInfo` to launch processes and explicitly set `UseShellExecute = false` and `CreateNoWindow = true` (unless shell execution or window visibility are absolutely necessary and input is strictly sanitized).
## 2024-05-14 - [CRITICAL] Prevent Path Hijacking in System Utility Execution
**Vulnerability:** Found insecure usage of relative paths / bare executable names (e.g., `sc.exe`, `wmic`, `powercfg`, `explorer.exe`) in `ProcessStartInfo` to launch system utilities.
**Learning:** Using relative paths allows malicious actors to place a rogue executable with the same name in a directory that occurs earlier in the system's `PATH` environment variable, leading to Local Privilege Escalation (LPE) or unintended code execution, especially when the process is executed with elevated privileges.
**Prevention:** Always use absolute, fully-qualified paths constructed securely using `Environment.GetFolderPath(Environment.SpecialFolder.System)` (or `.Windows`) combined with `Path.Combine` when starting system utilities via `Process.Start`.
## 2024-10-24 - Path Hijacking in Process Recovery
**Vulnerability:** Process paths are not captured safely before process.Kill(), meaning Process.Start(processName) is vulnerable to path hijacking.
**Learning:** Always use process.MainModule?.FileName safely before calling process.Kill().
**Prevention:** Capture absolute path with Win32Exception/InvalidOperationException handling.
