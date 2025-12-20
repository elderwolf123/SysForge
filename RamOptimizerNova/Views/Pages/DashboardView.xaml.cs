using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using RamOptimizerNova.ViewModels.Pages;

namespace RamOptimizerNova.Views.Pages;

public partial class DashboardView : UserControl
{
    private DashboardViewModel? _viewModel;

    public DashboardView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Get the view model
        _viewModel = this.DataContext as DashboardViewModel;
        
        // Subscribe to view model events
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.InitializeAsync().FireAndForget();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        // Ensure view model is initialized when attached to visual tree
        if (_viewModel != null && !_viewModel.IsInitialized)
        {
            _viewModel.InitializeAsync().FireAndForget();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        // Clean up view model
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.CleanupAsync().FireAndForget();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Handle property changes if needed
        switch (e.PropertyName)
        {
            case nameof(DashboardViewModel.IsRefreshing):
                HandleRefreshStateChange();
                break;
            case nameof(DashboardViewModel.ViewMode):
                HandleViewModeChange();
                break;
            case nameof(DashboardViewModel.ShowProcesses):
                HandleProcessesVisibilityChange();
                break;
            case nameof(DashboardViewModel.ShowAlerts):
                HandleAlertsVisibilityChange();
                break;
            case nameof(DashboardViewModel.ShowQuickActions):
                HandleQuickActionsVisibilityChange();
                break;
        }
    }

    private void HandleRefreshStateChange()
    {
        if (_viewModel == null) return;
        
        Dispatcher.UIThread.Post(() =>
        {
            // Update UI based on refresh state
            var refreshButton = this.Find<Button>("RefreshButton");
            if (refreshButton != null)
            {
                refreshButton.IsEnabled = !_viewModel.IsRefreshing;
            }
            
            // Show/hide loading indicators
            UpdateLoadingIndicators(_viewModel.IsRefreshing);
        });
    }

    private void HandleViewModeChange()
    {
        if (_viewModel == null) return;
        
        Dispatcher.UIThread.Post(() =>
        {
            // Update UI based on view mode
            var metricCards = this.Find<Panel>("MetricCardsPanel");
            if (metricCards != null)
            {
                if (_viewModel.ViewMode == DashboardViewMode.Minimal)
                {
                    // Hide some elements in minimal mode
                    metricCards.Margin = new Thickness(0, 0, 0, 12);
                }
                else
                {
                    // Show all elements in comprehensive mode
                    metricCards.Margin = new Thickness(0, 0, 12, 12);
                }
            }
        });
    }

    private void HandleProcessesVisibilityChange()
    {
        if (_viewModel == null) return;
        
        Dispatcher.UIThread.Post(() =>
        {
            var processesSection = this.Find<Border>("ProcessesSection");
            if (processesSection != null)
            {
                processesSection.Visibility = _viewModel.ShowProcesses ? 
                    Avalonia.Controls.Visibility.Visible : Avalonia.Controls.Visibility.Collapsed;
            }
        });
    }

    private void HandleAlertsVisibilityChange()
    {
        if (_viewModel == null) return;
        
        Dispatcher.UIThread.Post(() =>
        {
            var alertsSection = this.Find<Border>("AlertsSection");
            if (alertsSection != null)
            {
                alertsSection.Visibility = _viewModel.ShowAlerts ? 
                    Avalonia.Controls.Visibility.Visible : Avalonia.Controls.Visibility.Collapsed;
            }
        });
    }

    private void HandleQuickActionsVisibilityChange()
    {
        if (_viewModel == null) return;
        
        Dispatcher.UIThread.Post(() =>
        {
            var quickActionsSection = this.Find<Border>("QuickActionsSection");
            if (quickActionsSection != null)
            {
                quickActionsSection.Visibility = _viewModel.ShowQuickActions ? 
                    Avalonia.Controls.Visibility.Visible : Avalonia.Controls.Visibility.Collapsed;
            }
        });
    }

    private void UpdateLoadingIndicators(bool isLoading)
    {
        // Update loading indicators based on refresh state
        var loadingOverlay = this.Find<Grid>("LoadingOverlay");
        if (loadingOverlay != null)
        {
            loadingOverlay.IsVisible = isLoading;
        }
        
        var loadingText = this.Find<TextBlock>("LoadingText");
        if (loadingText != null)
        {
            loadingText.Text = isLoading ? "Refreshing..." : "Ready";
        }
    }

    private void OnProcessItemClicked(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null || sender is not Border border) return;
        
        try
        {
            // Get process data from the border's data context
            var processItem = border.DataContext;
            if (processItem != null)
            {
                // Navigate to process details or show dialog
                _viewModel.ViewProcessDetailsCommand.Execute(new ViewProcessDetailsEventArgs(
                    (int)processItem.GetType().GetProperty("Id")?.GetValue(processItem) ?? 0
                ));
            }
        }
        catch (Exception ex)
        {
            // Log error
            System.Diagnostics.Debug.WriteLine($"Error handling process item click: {ex.Message}");
        }
    }

    private void OnAlertItemClicked(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null || sender is not Border border) return;
        
        try
        {
            // Get alert data from the border's data context
            var alertItem = border.DataContext;
            if (alertItem != null)
            {
                // Navigate to alert details or show dialog
                _viewModel.ViewAlertDetailsCommand.Execute(new ViewAlertDetailsEventArgs(
                    (string)alertItem.GetType().GetProperty("Title")?.GetValue(alertItem) ?? ""
                ));
            }
        }
        catch (Exception ex)
        {
            // Log error
            System.Diagnostics.Debug.WriteLine($"Error handling alert item click: {ex.Message}");
        }
    }

    private void OnQuickActionClicked(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null || sender is not Button button) return;
        
        try
        {
            // Get action parameter from button's command parameter
            var action = button.CommandParameter as string;
            if (!string.IsNullOrEmpty(action))
            {
                _viewModel.QuickActionCommand.Execute(action);
            }
        }
        catch (Exception ex)
        {
            // Log error
            System.Diagnostics.Debug.WriteLine($"Error handling quick action click: {ex.Message}");
        }
    }

    private void OnChartTypeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_viewModel == null || sender is not ComboBox comboBox) return;
        
        try
        {
            var selectedItem = comboBox.SelectedItem;
            if (selectedItem != null)
            {
                var chartType = selectedItem.ToString();
                if (!string.IsNullOrEmpty(chartType))
                {
                    _viewModel.ChangeChartTypeCommand.Execute(chartType);
                }
            }
        }
        catch (Exception ex)
        {
            // Log error
            System.Diagnostics.Debug.WriteLine($"Error handling chart type change: {ex.Message}");
        }
    }

    private void OnRefreshClicked(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.RefreshCommand.Execute(null);
        }
    }

    private void OnOptimizeNowClicked(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.OptimizeNowCommand.Execute(null);
        }
    }

    private void OnViewModeToggled(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.ChangeViewModeCommand.Execute(null);
        }
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_viewModel == null || sender is not TextBox textBox) return;
        
        try
        {
            _viewModel.SearchText = textBox.Text;
        }
        catch (Exception ex)
        {
            // Log error
            System.Diagnostics.Debug.WriteLine($"Error handling search text change: {ex.Message}");
        }
    }

    // Public method for external access to refresh
    public async Task RefreshAsync()
    {
        if (_viewModel != null)
        {
            await _viewModel.RefreshAsync();
        }
    }

    // Public method for external access to focus search
    public async Task FocusSearchAsync()
    {
        if (_viewModel != null)
        {
            await _viewModel.FocusSearchAsync();
        }
    }

    // Public method for external access to handle theme changes
    public async Task OnThemeChangedAsync(ThemeVariant newTheme)
    {
        if (_viewModel != null)
        {
            await _viewModel.OnThemeChangedAsync(newTheme);
        }
    }
}

// Extension method for fire and forget
internal static class TaskExtensions
{
    public static void FireAndForget(this Task task)
    {
        // Fire and forget the task without awaiting
        _ = task.ContinueWith(t =>
        {
            // Handle exceptions if needed
            if (t.IsFaulted)
            {
                System.Diagnostics.Debug.WriteLine($"Task faulted: {t.Exception}");
            }
        }, TaskContinuationOptions.ExecuteSynchronously);
    }
}

// Supporting classes for event arguments
public class ViewProcessDetailsEventArgs
{
    public int ProcessId { get; }

    public ViewProcessDetailsEventArgs(int processId)
    {
        ProcessId = processId;
    }
}

public class ViewAlertDetailsEventArgs
{
    public string AlertTitle { get; }

    public ViewAlertDetailsEventArgs(string alertTitle)
    {
        AlertTitle = alertTitle;
    }
}

// Enum for view modes
public enum DashboardViewMode
{
    Comprehensive,
    Minimal
}