## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2024-05-14 - [Process Monitoring Loop Bottleneck]
**Learning:** Calling `Process.GetProcessesByName()` inside a loop iterates over all system processes each time, leading to O(M*N) complexity. Furthermore, passing process names with `.exe` extensions fails to match on Windows.
**Action:** Fetch `Process.GetProcesses()` once outside the loop, store `ProcessName`s in a `HashSet<string>(StringComparer.OrdinalIgnoreCase)`, remove `.exe` extensions from target names, and perform O(1) `.Contains()` lookups instead.
