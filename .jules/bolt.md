## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2024-05-15 - [Process Name Loop Bottleneck]
**Learning:** Found an O(M*N) complexity bottleneck in `SystemStabilityTester.CheckCriticalProcesses()`. It iterates over a list of process names and calls `Process.GetProcessesByName()` inside the loop. This internally iterates over all system processes each time. Also, process names fetched via `Process.GetProcessesByName()` should not include the `.exe` extension.
**Action:** Call `Process.GetProcesses()` once outside the loop to fetch all running processes, store their `ProcessName` properties in a `HashSet<string>` with `StringComparer.OrdinalIgnoreCase` for O(1) lookups, and ensure hardcoded process names do not include `.exe` extensions.
