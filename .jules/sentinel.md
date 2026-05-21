## 2024-05-09 - [CRITICAL] Prevent Command Injection in Process Executions
**Vulnerability:** Found insecure usage of `Process.Start(string)` and `ProcessStartInfo` where processes were launched without explicit restrictions, notably leaving `UseShellExecute` as default or `true`, which creates severe command injection and execution control risks.
**Learning:** `Process.Start` without enforcing `UseShellExecute = false` allows commands to be interpreted by the operating system shell. In contexts where process names or paths can be manipulated or aren't strictly hardcoded, this poses a risk of executing arbitrary commands.
**Prevention:** Always use `ProcessStartInfo` to launch processes and explicitly set `UseShellExecute = false` and `CreateNoWindow = true` (unless shell execution or window visibility are absolutely necessary and input is strictly sanitized).

## 2024-05-21 - [Prevent Path Hijacking in Elevated Processes]
**Vulnerability:** Found unqualified executable paths (e.g., `"sc.exe"`) used in `Process.Start` within code paths that require and run with Administrator privileges (`AsusServiceManager.cs`).
**Learning:** This is a critical Local Privilege Escalation (LPE) risk via Path Hijacking. If an attacker places a malicious executable with the same name earlier in the system's `PATH` variable, the application will execute it with elevated privileges.
**Prevention:** Always use absolute, fully-qualified paths via `Environment.GetFolderPath(Environment.SpecialFolder.System)` when starting system utilities (like `sc.exe`, `wmic.exe`, `powercfg.exe`), especially in contexts with elevated privileges.
