## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.
## 2024-05-24 - [Avoid String Allocations in Dictionary Lookups]
**Learning:** Calling `.ToLower()` during dictionary lookups (e.g., `TryGetValue(key.ToLower(), ...)`) allocates new string objects on the heap, causing unnecessary GC pressure.
**Action:** Initialize `Dictionary<string, T>` with `StringComparer.OrdinalIgnoreCase` instead, allowing case-insensitive lookups with zero allocation overhead.
