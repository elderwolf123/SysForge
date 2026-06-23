## 2025-02-27 - [Avoid Directory.GetFiles for whole-drive traversal]
**Learning:** [Using `Directory.GetFiles` for traversing an entire drive eagerly loads all paths into memory before returning, causing enormous memory consumption and massive garbage collection overhead in compression utilities.]
**Action:** [Always use `Directory.EnumerateFiles` when working with potentially large directories or scanning full drives. This allows the system to yield file paths lazily, avoiding array allocations for millions of files.]
