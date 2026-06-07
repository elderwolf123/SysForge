## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.
## 2024-05-14 - [Dictionary Key String Allocation Bottleneck]
**Learning:** Initializing dictionaries with strings and then querying them by appending `.ToLower()` on incoming keys creates unnecessary string allocations on the heap for every lookup.
**Action:** Use `new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase)` during initialization to bypass the need to `.ToLower()` at the lookup site.
