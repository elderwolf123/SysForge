using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RamOptimizerConsole.ConsoleUI;

/// <summary>
* Professional console styling with animations and visual effects
/// </summary>
public static class ProfessionalConsoleStyling
{
    private static readonly Random _random = new();
    private static int _animationFrame = 0;

    /// <summary>
    * Display animated title
    /// </summary>
    public static void DisplayAnimatedTitle(string title, int delayMs = 100)
    {
        var colors = new[] { ConsoleColor.Cyan, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.Yellow };
        
        for (int i = 0; i < title.Length; i++)
        {
            Console.ForegroundColor = colors[i % colors.Length];
            Console.Write(title[i]);
            System.Threading.Thread.Sleep(delayMs);
        }
        
        Console.ResetColor();
        Console.WriteLine();
    }

    /// <summary>
    * Display typewriter effect
    /// </summary>
    public static void DisplayTypewriter(string text, int delayMs = 50)
    {
        foreach (char c in text)
        {
            Console.Write(c);
            System.Threading.Thread.Sleep(delayMs);
        }
        Console.WriteLine();
    }

    /// <summary>
    * Display rainbow text
    /// </summary>
    public static void DisplayRainbow(string text)
    {
        var colors = new[] { ConsoleColor.Red, ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.Cyan, ConsoleColor.Blue, ConsoleColor.Magenta };
        
        for (int i = 0; i < text.Length; i++)
        {
            Console.ForegroundColor = colors[i % colors.Length];
            Console.Write(text[i]);
        }
        
        Console.ResetColor();
        Console.WriteLine();
    }

    /// <summary>
    * Display matrix rain effect
    /// </summary>
    public static void DisplayMatrixRain(int lines = 20, int durationMs = 3000)
    {
        var chars = "01";
        var width = Console.WindowWidth;
        var columns = new int[width];
        
        // Initialize columns
        for (int i = 0; i < width; i++)
        {
            columns[i] = _random.Next(0, lines);
        }

        var startTime = DateTime.Now;
        while ((DateTime.Now - startTime).TotalMilliseconds < durationMs)
        {
            Console.SetCursorPosition(0, 0);
            
            for (int y = 0; y < lines; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y == columns[x])
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(chars[_random.Next(0, chars.Length)]);
                    }
                    else if (y > columns[x] && y <= columns[x] + 10)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.Write(chars[_random.Next(0, chars.Length)]);
                    }
                    else
                    {
                        Console.Write(" ");
                    }
                }
                Console.WriteLine();
            }
            
            // Update columns
            for (int x = 0; x < width; x++)
            {
                if (_random.Next(0, 100) < 5)
                {
                    columns[x] = 0;
                }
                else if (columns[x] < lines)
                {
                    columns[x]++;
                }
            }
            
            System.Threading.Thread.Sleep(50);
        }
        
        Console.Clear();
    }

    /// <summary>
    * Display loading animation
    /// </summary>
    public static void DisplayLoading(string message, int durationMs = 2000)
    {
        var spinner = new[] { '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏' };
        int spinnerIndex = 0;
        var startTime = DateTime.Now;
        
        while ((DateTime.Now - startTime).TotalMilliseconds < durationMs)
        {
            Console.Write($"\r{message} {spinner[spinnerIndex % spinner.Length]}");
            spinnerIndex++;
            System.Threading.Thread.Sleep(100);
        }
        
        Console.WriteLine();
    }

    /// <summary>
    * Display progress animation
    /// </summary>
    public static void DisplayProgressAnimation(string message, int steps = 20, int delayMs = 100)
    {
        Console.Write(message);
        
        for (int i = 0; i < steps; i++)
        {
            Console.Write("█");
            System.Threading.Thread.Sleep(delayMs);
        }
        
        Console.WriteLine();
    }

    /// <summary>
    * Display wave animation
    /// </summary>
    public static void DisplayWaveAnimation(string text, int durationMs = 2000)
    {
        var chars = text.ToCharArray();
        var colors = new[] { ConsoleColor.Cyan, ConsoleColor.Green, ConsoleColor.Yellow, ConsoleColor.Red };
        
        var startTime = DateTime.Now;
        while ((DateTime.Now - startTime).TotalMilliseconds < durationMs)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            
            for (int i = 0; i < chars.Length; i++)
            {
                int colorIndex = (i + _animationFrame) % colors.Length;
                Console.ForegroundColor = colors[colorIndex];
                Console.Write(chars[i]);
            }
            
            Console.ResetColor();
            Console.WriteLine();
            
            _animationFrame++;
            System.Threading.Thread.Sleep(100);
        }
        
        Console.Clear();
    }

    /// <summary>
    * Display pulse animation
    /// </summary>
    public static void DisplayPulseAnimation(string text, int durationMs = 2000)
    {
        var startTime = DateTime.Now;
        int pulseDirection = 1;
        int pulseIntensity = 0;
        
        while ((DateTime.Now - startTime).TotalMilliseconds < durationMs)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            
            // Calculate pulse effect
            pulseIntensity += pulseDirection;
            if (pulseIntensity >= 10 || pulseIntensity <= 0)
            {
                pulseDirection *= -1;
            }
            
            // Display text with pulse effect
            for (int i = 0; i < text.Length; i++)
            {
                if (i % 2 == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                
                // Add spacing for pulse effect
                for (int j = 0; j < pulseIntensity / 2; j++)
                {
                    Console.Write(" ");
                }
                
                Console.Write(text[i]);
            }
            
            Console.ResetColor();
            Console.WriteLine();
            
            System.Threading.Thread.Sleep(50);
        }
        
        Console.Clear();
    }

    /// <summary>
    * Display scan line effect
    /// </summary>
    public static void DisplayScanLineEffect(int durationMs = 3000)
    {
        var width = Console.WindowWidth;
        var height = Console.WindowHeight;
        var startTime = DateTime.Now;
        
        while ((DateTime.Now - startTime).TotalMilliseconds < durationMs)
        {
            int scanLine = _random.Next(0, height);
            
            Console.SetCursorPosition(0, scanLine);
            Console.ForegroundColor = ConsoleColor.Green;
            
            for (int x = 0; x < width; x++)
            {
                Console.Write("━");
            }
            
            Console.ResetColor();
            System.Threading.Thread.Sleep(50);
            
            // Clear scan line
            Console.SetCursorPosition(0, scanLine);
            for (int x = 0; x < width; x++)
            {
                Console.Write(" ");
            }
        }
        
        Console.Clear();
    }

    /// <summary>
    * Display digital rain effect
    /// </summary>
    public static void DisplayDigitalRain(int durationMs = 4000)
    {
        var chars = "01アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン";
        var width = Console.WindowWidth;
        var drops = new int[width];
        
        // Initialize drops
        for (int i = 0; i < width; i++)
        {
            drops[i] = _random.Next(-height, 0);
        }
        
        var startTime = DateTime.Now;
        while ((DateTime.Now - startTime).TotalMilliseconds < durationMs)
        {
            Console.SetCursorPosition(0, 0);
            
            for (int i = 0; i < width; i++)
            {
                if (drops[i] < height && drops[i] >= 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(chars[_random.Next(0, chars.Length)]);
                    
                    if (drops[i] > 0)
                    {
                        Console.SetCursorPosition(i, drops[i] - 1);
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.Write(chars[_random.Next(0, chars.Length)]);
                    }
                }
                else
                {
                    Console.Write(" ");
                }
            }
            
            // Update drops
            for (int i = 0; i < width; i++)
            {
                if (drops[i] < height && drops[i] >= 0)
                {
                    drops[i]++;
                }
                else if (_random.Next(0, 100) < 2)
                {
                    drops[i] = 0;
                }
            }
            
            System.Threading.Thread.Sleep(50);
        }
        
        Console.Clear();
    }

    /// <summary>
    * Display professional separator with animation
    /// </summary>
    public static void DisplayAnimatedSeparator(char separator = '─', int width = 80, int durationMs = 1000)
    {
        var colors = new[] { ConsoleColor.Cyan, ConsoleColor.Green, ConsoleColor.Yellow, ConsoleColor.Red };
        var startTime = DateTime.Now;
        int colorIndex = 0;
        
        while ((DateTime.Now - startTime).TotalMilliseconds < durationMs)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            
            Console.ForegroundColor = colors[colorIndex % colors.Length];
            Console.WriteLine(new string(separator, width));
            
            colorIndex++;
            System.Threading.Thread.Sleep(100);
        }
        
        Console.ResetColor();
    }

    /// <summary>
    * Display blinking text
    /// </summary>
    public static void DisplayBlinking(string text, int durationMs = 2000, int blinkIntervalMs = 200)
    {
        var startTime = DateTime.Now;
        bool visible = true;
        
        while ((DateTime.Now - startTime).TotalMilliseconds < durationMs)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            
            if (visible)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(text);
            }
            else
            {
                Console.WriteLine(new string(' ', text.Length));
            }
            
            Console.ResetColor();
            visible = !visible;
            System.Threading.Thread.Sleep(blinkIntervalMs);
        }
        
        Console.Clear();
    }

    /// <summary>
    * Display gradient text
    /// </summary>
    public static void DisplayGradient(string text, ConsoleColor startColor, ConsoleColor endColor)
    {
        var colors = GetGradientColors(startColor, endColor, text.Length);
        
        for (int i = 0; i < text.Length; i++)
        {
            Console.ForegroundColor = colors[i];
            Console.Write(text[i]);
        }
        
        Console.ResetColor();
        Console.WriteLine();
    }

    /// <summary>
    * Get gradient colors
    /// </summary>
    private static ConsoleColor[] GetGradientColors(ConsoleColor start, ConsoleColor end, int steps)
    {
        var colors = new ConsoleColor[steps];
        
        // Simple color interpolation (this is a simplified approach)
        for (int i = 0; i < steps; i++)
        {
            double ratio = (double)i / (steps - 1);
            
            if (ratio < 0.5)
            {
                colors[i] = start;
            }
            else
            {
                colors[i] = end;
            }
        }
        
        return colors;
    }

    /// <summary>
    * Display ASCII art
    /// </summary>
    public static void DisplayAsciiArt(string[] artLines)
    {
        foreach (var line in artLines)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(line);
        }
        Console.ResetColor();
    }

    /// <summary>
    * Create professional ASCII art for RAM Optimizer
    /// </summary>
    public static string[] GetRamOptimizerArt()
    {
        return new[]
        {
            "    ╔══════════════════════════════════════════════════════════════════╗",
            "    ║                                                                  ║",
            "    ║    ██████╗ ███████╗ █████╗ ████████╗ ██████╗ ██████╗ ███████╗      ║",
            "    ║   ██╔════╝ ██╔════╝██╔══██╗╚══██╔══╝██╔═══██╗██╔══██╗██╔════╝      ║",
            "    ║   ██║  ███╗█████╗  ███████║   ██║   ██║   ██║██████╔╝█████╗        ║",
            "    ║   ██║   ██║██╔══╝  ██╔══██║   ██║   ██║   ██║██╔══██╗██╔══╝        ║",
            "    ║   ╚██████╔╝███████╗██║  ██║   ██║   ╚██████╔╝██║  ██║███████╗      ║",
            "    ║    ╚═════╝ ╚══════╝╚═╝  ╚═╝   ╚═╝    ╚═════╝ ╚═╝  ╚═╝╚══════╝      ║",
            "    ║                                                                  ║",
            "    ║                    PROFESSIONAL SYSTEM OPTIMIZATION                ║",
            "    ║                                                                  ║",
            "    ╚══════════════════════════════════════════════════════════════════╝"
        };
    }
}