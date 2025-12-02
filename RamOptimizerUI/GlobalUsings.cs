// Global using directives for RamOptimizerUI
// This file explicitly controls what namespaces are available globally
// to avoid ambiguity between WPF and WinForms types

// ===== Common System namespaces =====
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using System.IO;

// ===== WPF namespaces (primary UI framework) =====
global using System.Windows;
global using System.Windows.Controls;
global using System.Windows.Media;
global using System.Windows.Data;

// ===== Explicit aliases to avoid ambiguity with WinForms =====
global using MessageBox = System.Windows.MessageBox;
global using UserControl = System.Windows.Controls.UserControl;
global using TextBox = System.Windows.Controls.TextBox;
global using Button = System.Windows.Controls.Button;
global using Color = System.Windows.Media.Color;

// ===== RamOptimizer Core namespaces =====
// These are used across many files in the UI project
global using RamOptimizer.Core;
global using RamOptimizer.Core.Interfaces;

// Note: System.Windows.Forms is NOT globally imported
// Files that need WinForms types (FolderBrowserDialog, NotifyIcon, etc.) 
// should explicitly add: using System.Windows.Forms;

// Note: Specific RamOptimizer modules (Compression, Monitoring, etc.) 
// are NOT globally imported to maintain clarity about dependencies
// Add them explicitly in files that need them
