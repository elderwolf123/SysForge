## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2024-05-14 - O(1) Lookups in Process Management
**Learning:** Checking against process exclusion lists is a performance hotspot because it happens inside monitoring loops that scan hundreds of processes. Using a `List<string>` and calling `.ToLower()` on every string allocation inside loops is a major anti-pattern that creates garbage and runs in O(N) time.
**Action:** Replace `List<string>` with `HashSet<string>(StringComparer.OrdinalIgnoreCase)` for exclusion lists. This eliminates the need for `.ToLower()` string allocations and changes lookup time from O(N) to O(1). Be careful when initializing not to include `.Select(s => s.ToLower()).ToList();` at the end of the collection initializer.
