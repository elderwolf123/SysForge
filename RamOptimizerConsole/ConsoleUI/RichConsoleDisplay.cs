using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RamOptimizerConsole.ConsoleUI;

/// <summary>
/// Enhanced console display system with colors, progress bars, and professional styling
/// </summary>
public static class RichConsoleDisplay
{
    // Color definitions
    public static readonly ConsoleColor DefaultColor = ConsoleColor.Gray;
    public static readonly ConsoleColor AccentColor = ConsoleColor.Cyan;
    public static readonly ConsoleColor SuccessColor = ConsoleColor.Green;
    public static readonly ConsoleColor WarningColor = ConsoleColor.Yellow;
    public static readonly ConsoleColor ErrorColor = ConsoleColor.Red;
    public static readonly ConsoleColor InfoColor = ConsoleColor.Blue;
    public static readonly ConsoleColor HighlightColor = ConsoleColor.Magenta;

    /// <summary>
    /// Display professional header
    /// </summary>
    public static void DisplayHeader(string title, string subtitle = "")
    {
        Console.Clear();
        Console.ForegroundColor = AccentColor;
        
        // Top border
        Console.WriteLine("╔" + new string('═', 78) + "╗");
        
        // Title
        Console.WriteLine("║" + CenterText(title, 78) + "║");
        
        if (!string.IsNullOrEmpty(subtitle))
        {
            Console.ForegroundColor = InfoColor;
            Console.WriteLine("║" + CenterText(subtitle, 78) + "║");
        }
        
        // Bottom border
        Console.ForegroundColor = AccentColor;
        Console.WriteLine("╚" + new string('═', 78) + "╝");
        
        Console.ResetColor();
        Console.WriteLine();
    }

    /// <summary>
    /// Display section header
    /// </summary>
    public static void DisplaySection(string sectionName)
    {
        Console.ForegroundColor = AccentColor;
        Console.WriteLine($"┌─ {sectionName} ─" + new string('─', Math.Max(0, 72 - sectionName.Length)));
        Console.ResetColor();
        Console.WriteLine();
    }

    /// <summary>
    /// Display success message
    /// </summary>
    public static void DisplaySuccess(string message)
    {
        Console.ForegroundColor = SuccessColor;
        Console.WriteLine($"✓ {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Display warning message
    /// </summary>
    public static void DisplayWarning(string message)
    {
        Console.ForegroundColor = WarningColor;
        Console.WriteLine($"⚠ {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Display error message
    /// </summary>
    public static void DisplayError(string message)
    {
        Console.ForegroundColor = ErrorColor;
        Console.WriteLine($"✗ {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Display info message
    /// </summary>
    public static void DisplayInfo(string message)
    {
        Console.ForegroundColor = InfoColor;
        Console.WriteLine($"ℹ {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Display progress bar
    /// </summary>
    public static void DisplayProgressBar(string label, int progress, int total, int width = 50)
    {
        if (total == 0) total = 1;
        
        int percentage = (progress * 100) / total;
        int filledWidth = (progress * width) / total;
        
        Console.Write($"{label} [");
        Console.ForegroundColor = SuccessColor;
        Console.Write(new string('█', filledWidth));
        Console.ResetColor();
        Console.Write(new string('░', width - filledWidth));
        Console.Write($"] {percentage}% ({progress}/{total})");
        
        if (progress == total)
        {
            Console.ForegroundColor = SuccessColor;
            Console.Write(" ✓");
            Console.ResetColor();
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// Display animated progress bar
    /// </summary>
    public static void DisplayAnimatedProgressBar(string label, int progress, int total, int width = 50)
    {
        if (total == 0) total = 1;
        
        int percentage = (progress * 100) / total;
        int filledWidth = (progress * width) / total;
        
        Console.Write($"\r{label} [");
        Console.ForegroundColor = SuccessColor;
        Console.Write(new string('█', filledWidth));
        Console.ResetColor();
        Console.Write(new string('░', width - filledWidth));
        Console.Write($"] {percentage}% ({progress}/{total})");
        
        if (progress == total)
        {
            Console.ForegroundColor = SuccessColor;
            Console.Write(" ✓");
            Console.ResetColor();
            Console.WriteLine();
        }
        else
        {
            // Add spinner animation
            char[] spinner = { '|', '/', '-', '\\' };
            int spinnerIndex = (progress * 4) / total;
            Console.Write($" {spinner[spinnerIndex % 4]}");
        }
        
        Console.SetCursorPosition(0, Console.CursorTop);
    }

    /// <summary>
    /// Display data table
    /// </summary>
    public static void DisplayTable(string[] headers, List<string[]> rows)
    {
        if (rows.Count == 0) return;

        // Calculate column widths
        int[] columnWidths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
        {
            columnWidths[i] = headers[i].Length;
            foreach (var row in rows)
            {
                if (i < row.Length && row[i].Length > columnWidths[i])
                {
                    columnWidths[i] = row[i].Length;
                }
            }
        }

        // Display headers
        Console.ForegroundColor = AccentColor;
        for (int i = 0; i < headers.Length; i++)
        {
            Console.Write(PadRight(headers[i], columnWidths[i]) + "  ");
        }
        Console.WriteLine();

        // Display separator
        Console.Write(new string('─', columnWidths.Sum() + (headers.Length - 1) * 2));
        Console.WriteLine();

        // Display rows
        Console.ResetColor();
        foreach (var row in rows)
        {
            for (int i = 0; i < Math.Min(row.Length, headers.Length); i++)
            {
                Console.Write(PadRight(row[i], columnWidths[i]) + "  ");
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Display menu with options
    /// </summary>
    public static void DisplayMenu(string title, List<string> options, int selectedIndex = 0)
    {
        DisplaySection(title);
        
        for (int i = 0; i < options.Count; i++)
        {
            if (i == selectedIndex)
            {
                Console.ForegroundColor = HighlightColor;
                Console.Write("▶ ");
                Console.ResetColor();
                Console.ForegroundColor = SuccessColor;
                Console.WriteLine(options[i]);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"  {options[i]}");
            }
        }
        
        Console.WriteLine();
        Console.ForegroundColor = InfoColor;
        Console.WriteLine("Use ↑/↓ arrows to navigate, Enter to select, ESC to go back");
        Console.ResetColor();
    }

    /// <summary>
    /// Display interactive menu with hotkeys
    /// </summary>
    public static void DisplayInteractiveMenu(string title, Dictionary<char, string> options)
    {
        DisplaySection(title);
        
        foreach (var option in options.OrderBy(k => k.Key))
        {
            Console.ForegroundColor = AccentColor;
            Console.Write($"[{option.Key}] ");
            Console.ResetColor();
            Console.WriteLine(option.Value);
        }
        
        Console.WriteLine();
        Console.ForegroundColor = InfoColor;
        Console.WriteLine("Press the corresponding key to select an option");
        Console.ResetColor();
    }

    /// <summary>
    /// Display status with spinner animation
    /// </summary>
    public static void DisplayStatus(string message, bool animate = true)
    {
        Console.Write(message);
        
        if (animate)
        {
            char[] spinner = { '|', '/', '-', '\\' };
            int spinnerIndex = 0;
            
            while (true)
            {
                Console.Write($"\r{message} {spinner[spinnerIndex % 4]}");
                spinnerIndex++;
                System.Threading.Thread.Sleep(100);
                
                if (!Console.KeyAvailable) continue;
                
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Escape)
                {
                    break;
                }
            }
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// Display file size in human readable format
    /// </summary>
    public static void DisplayFileSize(long bytes, string label = "")
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        
        string formatted = $"{len:0.##} {sizes[order]}";
        
        if (!string.IsNullOrEmpty(label))
        {
            Console.Write($"{label}: ");
        }
        
        Console.ForegroundColor = HighlightColor;
        Console.Write(formatted);
        Console.ResetColor();
    }

    /// <summary>
    * Display percentage with color coding
    /// </summary>
    public static void DisplayPercentage(double value, string label = "")
    {
        string formatted = $"{value:P0}";
        
        if (!string.IsNullOrEmpty(label))
        {
            Console.Write($"{label}: ");
        }
        
        if (value >= 0.8)
        {
            Console.ForegroundColor = SuccessColor;
        }
        else if (value >= 0.5)
        {
            Console.ForegroundColor = WarningColor;
        }
        else
        {
            Console.ForegroundColor = ErrorColor;
        }
        
        Console.Write(formatted);
        Console.ResetColor();
    }

    /// <summary>
    /// Display separator line
    /// </summary>
    public static void DisplaySeparator(char character = '─')
    {
        Console.ForegroundColor = AccentColor;
        Console.WriteLine(new string(character, 80));
        Console.ResetColor();
    }

    /// <summary>
    /// Display footer
    /// </summary>
    public static void DisplayFooter()
    {
        DisplaySeparator();
        Console.ForegroundColor = InfoColor;
        Console.WriteLine("RAM Optimizer Nova - Professional System Optimization Suite");
        Console.ResetColor();
    }

    // Helper methods
    private static string CenterText(string text, int width)
    {
        if (text.Length >= width) return text;
        
        int leftPadding = (width - text.Length) / 2;
        int rightPadding = width - text.Length - leftPadding;
        
        return new string(' ', leftPadding) + text + new string(' ', rightPadding);
    }

    private static string PadRight(string text, int width)
    {
        if (text.Length >= width) return text;
        return text.PadRight(width);
    }
}