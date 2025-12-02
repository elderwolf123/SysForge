using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace RamOptimizerUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private Mutex _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Ensure only one instance of the application is running
            bool createdNew;
            _mutex = new Mutex(true, "RamOptimizerPro", out createdNew);

            if (!createdNew)
            {
                // Application is already running
                MessageBox.Show("RAM Optimizer Pro is already running.", "Application Running", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Find and activate the existing window
                foreach (Window window in Windows)
                {
                    window.Activate();
                    if (window.WindowState == WindowState.Minimized)
                    {
                        window.WindowState = WindowState.Normal;
                    }
                    break;
                }
                
                Shutdown();
                return;
            }

            // Set up exception handling
            SetupExceptionHandling();

            base.OnStartup(e);
        }

        private void SetupExceptionHandling()
        {
            // Handle exceptions on the UI thread
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            // Handle exceptions on background threads
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Log the exception
            Debug.WriteLine($"UI Thread Exception: {e.Exception}");
            
            // Show error message to user
            MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            
            // Prevent the application from crashing
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Log the exception
            var exception = e.ExceptionObject as Exception;
            Debug.WriteLine($"Background Thread Exception: {exception?.Message}");
            
            // In a real application, you would log this to a file or error reporting service
            // For now, we'll just show a message box if the application is not shutting down
            if (!e.IsTerminating)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"A background error occurred: {exception?.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up the mutex
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();

            base.OnExit(e);
        }
    }
}
