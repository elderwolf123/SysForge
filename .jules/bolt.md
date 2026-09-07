## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2026-09-07 - [O(M*N) process lookup bottleneck]
**Learning:** Calling `Process.GetProcessesByName()` inside a loop iterates through all system processes internally on each call, leading to an O(M*N) bottleneck. Additionally, passing names with `.exe` extension to it fails to match processes on Windows.
**Action:** Fetch `Process.GetProcesses()` once outside the loop and map them into a `HashSet<string>(StringComparer.OrdinalIgnoreCase)` for O(1) lookups, and ensure process names do not contain `.exe`.
