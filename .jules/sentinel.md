## 2024-05-09 - [CRITICAL] Prevent Command Injection in Process Executions
**Vulnerability:** Found insecure usage of `Process.Start(string)` and `ProcessStartInfo` where processes were launched without explicit restrictions, notably leaving `UseShellExecute` as default or `true`, which creates severe command injection and execution control risks.
**Learning:** `Process.Start` without enforcing `UseShellExecute = false` allows commands to be interpreted by the operating system shell. In contexts where process names or paths can be manipulated or aren't strictly hardcoded, this poses a risk of executing arbitrary commands.
**Prevention:** Always use `ProcessStartInfo` to launch processes and explicitly set `UseShellExecute = false` and `CreateNoWindow = true` (unless shell execution or window visibility are absolutely necessary and input is strictly sanitized).

## 2024-05-10 - [HIGH] Prevent Path Hijacking Vulnerabilities in Process Executions
**Vulnerability:** Found multiple instances where system utilities (like `wmic`, `powercfg`, `sc.exe`, and `explorer.exe`) were executed using relative paths or plain executable names.
**Learning:** Executing system utilities by name without absolute paths leaves the application vulnerable to path hijacking and local privilege escalation (LPE). An attacker could place a malicious executable with the same name earlier in the system's PATH, causing the application to execute the malicious payload instead of the intended system utility.
**Prevention:** Always use fully-qualified absolute paths when starting processes (e.g., `System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "wmic.exe")` or `Environment.SpecialFolder.Windows` for `explorer.exe`) to guarantee the correct executable is launched.
