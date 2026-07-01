## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2024-07-01 - [Directory Enumeration Performance]
**Learning:** `Directory.GetFiles` with `SearchOption.AllDirectories` allocates an entire array in memory before returning, which uses excessive memory for large directory trees and crashes completely with `UnauthorizedAccessException` if it encounters a protected system folder.
**Action:** Use `Directory.EnumerateFiles(path, "*", new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true })` instead for lazy evaluation and built-in exception handling.
