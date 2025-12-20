using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RamOptimizerConsole.ConsoleUI;

/// <summary>
/// Interactive console with hotkeys, real-time monitoring, and user input handling
/// </summary>
public static class InteractiveConsole
{
    private static readonly Dictionary<ConsoleKey, Action> _keyHandlers = new();
    private static readonly Dictionary<char, Action> _charHandlers = new();
    private static bool _isRunning = false;
    private static CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// Register a key handler
    /// </summary>
    public static void RegisterKeyHandler(ConsoleKey key, Action handler)
    {
        _keyHandlers[key] = handler;
    }

    /// <summary>
    /// Register a character handler
    /// </summary>
    public static void RegisterCharHandler(char key, Action handler)
    {
        _charHandlers[key] = handler;
    }

    /// <summary>
    /// Start interactive mode
    /// </summary>
    public static void StartInteractiveMode()
    {
        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();

        // Start background key listener
        Task.Run(() => ListenForKeyPresses(_cancellationTokenSource.Token));

        RichConsoleDisplay.DisplayInfo("Interactive mode started. Press ESC to exit.");
    }

    /// <summary>
    /// Stop interactive mode
    /// </summary>
    public static void StopInteractiveMode()
    {
        _isRunning = false;
        _cancellationTokenSource.Cancel();
        RichConsoleDisplay.DisplayInfo("Interactive mode stopped.");
    }

    /// <summary>
    /// Wait for user input with timeout
    /// </summary>
    public static async Task<string> WaitForInputAsync(int timeoutMs = 30000)
    {
        var cts = new CancellationTokenSource(timeoutMs);
        
        try
        {
            return await Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Enter)
                        {
                            return Console.ReadLine() ?? "";
                        }
                        else if (key.Key == ConsoleKey.Escape)
                        {
                            return "";
                        }
                    }
                    Thread.Sleep(100);
                }
                return "";
            }, cts.Token);
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Get user confirmation
    /// </summary>
    public static async Task<bool> GetConfirmationAsync(string message)
    {
        Console.Write($"{message} (Y/N): ");
        var input = await WaitForInputAsync(5000);
        return input.ToUpper() == "Y";
    }

    /// <summary>
    /// Get user choice from menu
    /// </summary>
    public static async Task<int> GetMenuChoiceAsync(List<string> options, int timeoutMs = 30000)
    {
        var selectedIndex = 0;
        var cts = new CancellationTokenSource(timeoutMs);

        // Display initial menu
        DisplayInteractiveMenu(options, selectedIndex);

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    
                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:
                            selectedIndex = Math.Max(0, selectedIndex - 1);
                            DisplayInteractiveMenu(options, selectedIndex);
                            break;
                        case ConsoleKey.DownArrow:
                            selectedIndex = Math.Min(options.Count - 1, selectedIndex + 1);
                            DisplayInteractiveMenu(options, selectedIndex);
                            break;
                        case ConsoleKey.Enter:
                            return selectedIndex;
                        case ConsoleKey.Escape:
                            return -1;
                    }
                }
                Thread.Sleep(50);
            }
        }
        catch
        {
            // Timeout
        }

        return selectedIndex;
    }

    /// <summary>
    * Get hotkey choice from options
    /// </summary>
    public static async Task<char> GetHotkeyChoiceAsync(Dictionary<char, string> options, int timeoutMs = 30000)
    {
        var cts = new CancellationTokenSource(timeoutMs);

        // Display menu
        RichConsoleDisplay.DisplayInteractiveMenu("Select Option", options);

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    char keyChar = char.ToUpper(key.KeyChar);
                    
                    if (options.ContainsKey(keyChar))
                    {
                        return keyChar;
                    }
                    else if (key.Key == ConsoleKey.Escape)
                    {
                        return '\0';
                    }
                }
                Thread.Sleep(50);
            }
        }
        catch
        {
            // Timeout
        }

        return '\0';
    }

    /// <summary>
    /// Display real-time monitoring
    /// </summary>
    public static void StartRealTimeMonitoring(Action<MonitorData> updateCallback, int updateIntervalMs = 1000)
    {
        Task.Run(async () =>
        {
            while (_isRunning)
            {
                var monitorData = new MonitorData
                {
                    Timestamp = DateTime.Now,
                    MemoryUsage = GetMemoryUsage(),
                    CpuUsage = GetCpuUsage(),
                    DiskUsage = GetDiskUsage(),
                    ActiveProcesses = GetActiveProcessCount()
                };

                updateCallback(monitorData);
                await Task.Delay(updateIntervalMs);
            }
        });
    }

    /// <summary>
    * Display animated status
    /// </summary>
    public static async Task DisplayAnimatedStatusAsync(string message, Func<bool> condition, int updateIntervalMs = 200)
    {
        var spinner = new[] { '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏' };
        int spinnerIndex = 0;

        while (!condition())
        {
            Console.Write($"\r{message} {spinner[spinnerIndex % spinner.Length]}");
            spinnerIndex++;
            await Task.Delay(updateIntervalMs);
        }

        Console.WriteLine();
    }

    /// <summary>
    * Display countdown
    /// </summary>
    public static async Task DisplayCountdownAsync(int seconds, string message = "Countdown")
    {
        for (int i = seconds; i > 0; i--)
        {
            Console.Write($"\r{message}: {i} seconds remaining...");
            await Task.Delay(1000);
        }
        Console.WriteLine();
    }

    /// <summary>
    * Listen for key presses in background
    /// </summary>
    private static async Task ListenForKeyPresses(CancellationToken cancellationToken)
    {
        while (_isRunning && !cancellationToken.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                
                if (_keyHandlers.TryGetValue(key.Key, out var handler))
                {
                    handler();
                }
                else if (key.KeyChar != '\0' && _charHandlers.TryGetValue(char.ToUpper(key.KeyChar), out var charHandler))
                {
                    charHandler();
                }
            }
            
            await Task.Delay(50);
        }
    }

    /// <summary>
    * Display interactive menu with selection
    /// </summary>
    private static void DisplayInteractiveMenu(List<string> options, int selectedIndex)
    {
        Console.SetCursorPosition(0, Console.CursorTop - options.Count - 2);
        
        for (int i = 0; i < options.Count; i++)
        {
            if (i == selectedIndex)
            {
                Console.ForegroundColor = RichConsoleDisplay.HighlightColor;
                Console.Write("▶ ");
                Console.ResetColor();
                Console.ForegroundColor = RichConsoleDisplay.SuccessColor;
                Console.WriteLine(options[i]);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"  {options[i]}");
            }
        }
        
        Console.SetCursorPosition(0, Console.CursorTop - options.Count);
    }

    // Helper methods for system monitoring
    private static double GetMemoryUsage()
    {
        try
        {
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            long memoryBytes = currentProcess.WorkingSet64;
            double memoryMB = memoryBytes / (1024.0 * 1024.0);
            
            // Get total system memory
            var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
            double totalMemoryMB = computerInfo.TotalPhysicalMemory / (1024.0 * 1024.0);
            
            return (memoryMB / totalMemoryMB) * 100;
        }
        catch
        {
            return 0;
        }
    }

    private static double GetCpuUsage()
    {
        try
        {
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            var startTime = DateTime.Now;
            var startCpuUsage = currentProcess.TotalProcessorTime;
            
            System.Threading.Thread.Sleep(500);
            
            var endTime = DateTime.Now;
            var endCpuUsage = currentProcess.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            
            return (cpuUsedMs / (Environment.ProcessorCount * totalMsPassed)) * 100;
        }
        catch
        {
            return 0;
        }
    }

    private static double GetDiskUsage()
    {
        try
        {
            var driveInfo = new System.IO.DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory));
            double totalSpace = driveInfo.TotalSize;
            double freeSpace = driveInfo.TotalFreeSpace;
            double usedSpace = totalSpace - freeSpace;
            
            return (usedSpace / totalSpace) * 100;
        }
        catch
        {
            return 0;
        }
    }

    private static int GetActiveProcessCount()
    {
        try
        {
            return System.Diagnostics.Process.GetProcesses().Length;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    * Monitor data structure
    /// </summary>
    public class MonitorData
    {
        public DateTime Timestamp { get; set; }
        public double MemoryUsage { get; set; }
        public double CpuUsage { get; set; }
        public double DiskUsage { get; set; }
        public int ActiveProcesses { get; set; }
    }
}