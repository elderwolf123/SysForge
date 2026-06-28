## 2024-05-09 - [Identifying Process Collection Bottleneck]
**Learning:** Found an O(N^2) or O(N * M) bottleneck in `ProcessPriorityManager.AdjustProcessPrioritiesAsync`. It gets all processes via `Process.GetProcesses()`, then for each process, it calls `exclusionList.Contains(process.ProcessName.ToLower())`. Since `exclusionList` is a `List<string>`, `Contains` does a linear scan for each process. Using a `HashSet<string>` with case-insensitive comparer makes this an O(1) lookup instead.
**Action:** Change `List<string> exclusionList` to `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` in `ProcessPriorityManager` and `InitializeExclusionList()`.
## 2024-05-18 - Optimize File System Traversal With EnumerationOptions

**Learning:** When using `Directory.EnumerateFiles(drive.Name, "*", SearchOption.AllDirectories)` across an entire drive, it may fail entirely if it encounters a single inaccessible directory (like System Volume Information) by throwing `UnauthorizedAccessException`.

**Action:** In .NET Core/.NET 5+, always utilize `new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true }` instead of `SearchOption.AllDirectories` to safely traverse file systems and smoothly skip over restricted folders.
