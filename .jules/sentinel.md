## 2024-05-09 - [CRITICAL] Prevent Command Injection in Process Executions
**Vulnerability:** Found insecure usage of `Process.Start(string)` and `ProcessStartInfo` where processes were launched without explicit restrictions, notably leaving `UseShellExecute` as default or `true`, which creates severe command injection and execution control risks.
**Learning:** `Process.Start` without enforcing `UseShellExecute = false` allows commands to be interpreted by the operating system shell. In contexts where process names or paths can be manipulated or aren't strictly hardcoded, this poses a risk of executing arbitrary commands.
**Prevention:** Always use `ProcessStartInfo` to launch processes and explicitly set `UseShellExecute = false` and `CreateNoWindow = true` (unless shell execution or window visibility are absolutely necessary and input is strictly sanitized).

## 2024-05-18 - [Path Hijacking in Process.Start]
**Vulnerability:** Invoking processes without absolute paths like `sc.exe`, `powercfg.exe`, `explorer.exe`, or `wmic.exe` via `Process.Start`.
**Learning:** These binaries are resolved using system PATH, allowing an attacker to plant a malicious executable with the same name in a directory earlier in the PATH (or working directory), which executes with the current application privileges.
**Prevention:** Always use fully-qualified absolute paths using `Environment.GetFolderPath` (e.g., `Environment.SpecialFolder.System` for `sc.exe`, `powercfg.exe` and `wbem\wmic.exe`; `Environment.SpecialFolder.Windows` for `explorer.exe`) to prevent binary planting/path hijacking.
