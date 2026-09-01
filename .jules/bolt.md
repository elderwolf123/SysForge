## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2024-05-14 - [Process Array Iteration Bottleneck]
**Learning:** Using `Process.GetProcessesByName()` inside a loop iterates over all system processes each time, leading to O(N*M) complexity. Additionally, it fails to match process names on Windows if the `.exe` extension is included.
**Action:** Fetch `Process.GetProcesses()` once outside loops, strip `.exe` from target lists, and filter using a `HashSet<string>` with `StringComparer.OrdinalIgnoreCase`.
