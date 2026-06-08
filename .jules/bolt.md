## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2024-05-10 - [Process Recovery Lookup Allocation]
**Learning:** Found string allocations in `ProcessRecoveryEngine.RecoverProcessAsync` where `processName.ToLower()` is called on every recovery evaluation. Dictionaries should use `StringComparer.OrdinalIgnoreCase` to handle case-insensitive string keys internally without generating temporary strings.
**Action:** Use `StringComparer.OrdinalIgnoreCase` during `Dictionary<string, T>` instantiation instead of `.ToLower()` for runtime string allocations, especially in execution paths related to process recovery and monitoring.
