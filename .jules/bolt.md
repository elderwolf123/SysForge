## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2026-05-29 - [Optimizing GPU Process Exclusions]
**Learning:** Found multiple instances where GPU optimization loops perform O(N) `List<string>.Contains` lookups alongside repetitive `.ToLower()` string allocations inside inner monitoring loops (e.g., `AdvancedGpuOptimizer`, `GpuOptimizer`, `GpuResourceOptimizer`). This creates unnecessary garbage collector pressure and slows down process verification.
**Action:** Replaced `List<string>` with `HashSet<string>(StringComparer.OrdinalIgnoreCase)` globally for exclusion lists in these GPU optimizer classes to upgrade lookups to O(1) and eliminate `.ToLower()` heap allocations.
