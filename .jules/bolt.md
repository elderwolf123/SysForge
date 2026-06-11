## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2024-05-10 - [Dictionary Allocation Optimization]
**Learning:** Found instances where string keys in `Dictionary<string, T>` were explicitly cast to lowercase `.ToLower()` during key insertions and lookups to support case-insensitivity. This causes heap allocations and garbage collection overhead. Using the `StringComparer.OrdinalIgnoreCase` comparer when initializing the dictionary handles casing intrinsically without generating new string objects.
**Action:** When initializing dictionaries intended for case-insensitive string lookups (like file extensions or process names), always pass `StringComparer.OrdinalIgnoreCase` into the constructor and remove `.ToLower()` calls during `.TryGetValue()`.
