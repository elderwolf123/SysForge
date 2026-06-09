## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.

## 2024-05-18 - StringComparer Optimization

**Learning:** When performing string lookups in C# Collections like Dictionary or HashSet, calling `.ToLower()` on the key generates a new string allocation every single time, putting unnecessary pressure on the Garbage Collector.

**Action:** Initialize collections with an appropriate `StringComparer` (like `StringComparer.OrdinalIgnoreCase`) directly so case-insensitive lookups occur without creating any new string objects.
