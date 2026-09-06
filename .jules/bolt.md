## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2024-05-10 - [Optimize ProcessRecoveryEngine Dictionary Lookups]
**Learning:** Found an unnecessary string allocation during dictionary lookups in `ProcessRecoveryEngine`. Calling `processName.ToLower()` on every lookup creates a new string on the heap, which generates GC pressure when called frequently.
**Action:** Changed the `Dictionary<string, List<RecoveryStrategy>>` initialization to use `StringComparer.OrdinalIgnoreCase`, which makes the key lookups inherently case-insensitive and O(1) without requiring any string allocation.
