## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2024-05-18 - Avoid Process Handle Leaks during O(1) Caching
**Learning:** When switching from `Process.GetProcessesByName()` in loops to `Process.GetProcesses()` outside loops to build an O(1) cache (`HashSet`), the array of returned `Process` objects hold unmanaged OS handles. Failing to call `.Dispose()` on these objects after projecting their names will cause temporary handle exhaustion and unnecessary GC pressure.
**Action:** Always wrap `Process` enumeration in a loop that explicitly calls `.Dispose()` on each instance once its data has been extracted, or use LINQ projections in tandem with manual disposal logic.
