## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2024-05-10 - [Optimize Dictionary Lookups with Case-Insensitive Comparer]
**Learning:** Initializing dictionaries containing lowercase keys and using `.ToLower()` in `TryGetValue` loop iterations causes unnecessary string allocations and garbage collection overhead. This creates micro-stutters in high-frequency loops.
**Action:** Always initialize case-insensitive string-keyed dictionaries using `new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase)`. Pass the original un-lowercased string key into `.TryGetValue(key, out _)`.
