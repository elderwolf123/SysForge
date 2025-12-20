using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using RamOptimizerNova.ViewModels;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.Views;

public partial class MainWindow : Window, IThemeAwareViewModel
{
    private readonly MainViewModel _viewModel;
    private readonly IThemeManager _themeManager;
    private readonly ISettingsService _settingsService;
    private bool _isMaximized = false;
    private Point _lastMousePosition;
    private bool _isDragging = false;

    public MainWindow()
    {
        InitializeComponent();

        // Get services from DI container
        _viewModel = (MainViewModel)DataContext;
        _themeManager = App.Current.Services.GetService<IThemeManager>();
        _settingsService = App.Current.Services.GetService<ISettingsService>();

        // Subscribe to theme change events
        _themeManager.ThemeChanged += OnThemeChanged;

        // Initialize window
        InitializeWindow();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void InitializeWindow()
    {
        try
        {
            // Set window icon
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                var iconStream = desktopLifetime.MainView?.Resources["AppIcon"] as Stream;
                if (iconStream != null)
                {
                    this.Icon = new Avalonia.Media.Imaging.Bitmap(iconStream);
                }
            }

            // Set window position and size from settings
            await LoadWindowSettingsAsync();

            // Subscribe to window events
            this.Closing += OnWindowClosing;
            this.Opened += OnWindowOpened;
            this.PropertyChanged += OnWindowPropertyChanged;

            // Initialize theme
            await _themeManager.InitializeAsync();

            // Subscribe to view model events
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.RequestClose += OnRequestClose;
            _viewModel.RequestMinimize += OnRequestMinimize;
            _viewModel.RequestMaximize += OnRequestMaximize;
            _viewModel.RequestRestore += OnRequestRestore;

            // Set up keyboard shortcuts
            SetupKeyboardShortcuts();

            // Set up mouse events for window dragging
            SetupMouseEvents();
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error initializing window: {ex.Message}");
        }
    }

    private async Task LoadWindowSettingsAsync()
    {
        try
        {
            var settings = await _settingsService.GetAllSettingsAsync();
            
            // Load window position
            var left = await _settingsService.GetSettingAsync<int>("WindowLeft", -1);
            var top = await _settingsService.GetSettingAsync<int>("WindowTop", -1);
            
            if (left >= 0 && top >= 0)
            {
                this.Position = new PixelPoint(left, top);
            }

            // Load window size
            var width = await _settingsService.GetSettingAsync<int>("WindowWidth", 1200);
            var height = await _settingsService.GetSettingAsync<int>("WindowHeight", 800);
            
            this.Width = width;
            this.Height = height;

            // Load window state
            var isMaximized = await _settingsService.GetSettingAsync<bool>("WindowMaximized", false);
            if (isMaximized)
            {
                this.WindowState = WindowState.Maximized;
                _isMaximized = true;
            }
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error loading window settings: {ex.Message}");
        }
    }

    private async Task SaveWindowSettingsAsync()
    {
        try
        {
            // Save window position
            await _settingsService.SetSettingAsync("WindowLeft", this.Position.X);
            await _settingsService.SetSettingAsync("WindowTop", this.Position.Y);

            // Save window size (only if not maximized)
            if (this.WindowState != WindowState.Maximized)
            {
                await _settingsService.SetSettingAsync("WindowWidth", (int)this.Width);
                await _settingsService.SetSettingAsync("WindowHeight", (int)this.Height);
            }

            // Save window state
            await _settingsService.SetSettingAsync("WindowMaximized", _isMaximized);
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error saving window settings: {ex.Message}");
        }
    }

    private void SetupKeyboardShortcuts()
    {
        // Add keyboard shortcuts
        this.KeyDown += async (sender, e) =>
        {
            try
            {
                switch (e.Key)
                {
                    case Key.F5:
                        // Refresh current page
                        await _viewModel.RefreshCurrentPageAsync();
                        break;
                    case Key.F11:
                        // Toggle fullscreen
                        await ToggleFullscreenAsync();
                        break;
                    case Key.Escape:
                        // Close current dialog or go back
                        await _viewModel.CloseCurrentDialogAsync();
                        break;
                    case Key.F:
                        if (e.KeyModifiers == KeyModifiers.Control)
                        {
                            // Focus search
                            await _viewModel.FocusSearchAsync();
                        }
                        break;
                    case Key.T:
                        if (e.KeyModifiers == KeyModifiers.Control)
                        {
                            // Toggle theme
                            await _themeManager.ToggleThemeAsync();
                        }
                        break;
                    case Key.R:
                        if (e.KeyModifiers == KeyModifiers.Control)
                        {
                            // Reload settings
                            await _viewModel.ReloadSettingsAsync();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                await _settingsService.LogAsync($"Error handling keyboard shortcut: {ex.Message}");
            }
        };
    }

    private void SetupMouseEvents()
    {
        // Title bar mouse events for window dragging
        var titleBar = this.Find<Border>("TitleBar");
        if (titleBar != null)
        {
            titleBar.PointerPressed += OnTitleBarPointerPressed;
            titleBar.PointerReleased += OnTitleBarPointerReleased;
            titleBar.PointerMoved += OnTitleBarPointerMoved;
            titleBar.PointerCaptureLost += OnTitleBarPointerCaptureLost;
        }

        // Window button events
        var minimizeButton = this.Find<Button>("MinimizeButton");
        if (minimizeButton != null)
        {
            minimizeButton.Click += OnMinimizeButtonClick;
        }

        var maximizeButton = this.Find<Button>("MaximizeButton");
        if (maximizeButton != null)
        {
            maximizeButton.Click += OnMaximizeButtonClick;
        }

        var closeButton = this.Find<Button>("CloseButton");
        if (closeButton != null)
        {
            closeButton.Click += OnCloseButtonClick;
        }
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            _lastMousePosition = e.GetPosition(this);
            _isDragging = true;
            this.BeginPointerCapture();
        }
        catch (Exception ex)
        {
            _settingsService.LogAsync($"Error handling title bar pointer pressed: {ex.Message}").Wait();
        }
    }

    private void OnTitleBarPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        try
        {
            _isDragging = false;
            this.EndPointerCapture();
        }
        catch (Exception ex)
        {
            _settingsService.LogAsync($"Error handling title bar pointer released: {ex.Message}").Wait();
        }
    }

    private void OnTitleBarPointerMoved(object? sender, PointerEventArgs e)
    {
        try
        {
            if (_isDragging)
            {
                var currentPosition = e.GetPosition(this);
                var delta = currentPosition - _lastMousePosition;

                var newX = this.Position.X + (int)delta.X;
                var newY = this.Position.Y + (int)delta.Y;

                // Keep window within screen bounds
                var screen = Screens.ScreenFromPoint(this.Position);
                if (screen != null)
                {
                    newX = Math.Max(screen.WorkingArea.X, Math.Min(newX, screen.WorkingArea.Right - (int)this.Width));
                    newY = Math.Max(screen.WorkingArea.Y, Math.Min(newY, screen.WorkingArea.Bottom - (int)this.Height));
                }

                this.Position = new PixelPoint(newX, newY);
                _lastMousePosition = currentPosition;
            }
        }
        catch (Exception ex)
        {
            _settingsService.LogAsync($"Error handling title bar pointer moved: {ex.Message}").Wait();
        }
    }

    private void OnTitleBarPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        try
        {
            _isDragging = false;
        }
        catch (Exception ex)
        {
            _settingsService.LogAsync($"Error handling title bar pointer capture lost: {ex.Message}").Wait();
        }
    }

    private async void OnMinimizeButtonClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.MinimizeWindowAsync();
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error minimizing window: {ex.Message}");
        }
    }

    private async void OnMaximizeButtonClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.MaximizeWindowAsync();
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error maximizing window: {ex.Message}");
        }
    }

    private async void OnCloseButtonClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.CloseWindowAsync();
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error closing window: {ex.Message}");
        }
    }

    private async Task ToggleFullscreenAsync()
    {
        try
        {
            if (this.WindowState == WindowState.FullScreen)
            {
                await _viewModel.RestoreWindowAsync();
            }
            else
            {
                await _viewModel.MaximizeWindowAsync();
            }
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error toggling fullscreen: {ex.Message}");
        }
    }

    private async void OnWindowOpened(object? sender, EventArgs e)
    {
        try
        {
            // Initialize view model
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error opening window: {ex.Message}");
        }
    }

    private async void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        try
        {
            // Save window settings
            await SaveWindowSettingsAsync();

            // Clean up view model
            await _viewModel.CleanupAsync();

            // Unsubscribe from events
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.RequestClose -= OnRequestClose;
            _viewModel.RequestMinimize -= OnRequestMinimize;
            _viewModel.RequestMaximize -= OnRequestMaximize;
            _viewModel.RequestRestore -= OnRequestRestore;

            _themeManager.ThemeChanged -= OnThemeChanged;
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error closing window: {ex.Message}");
        }
    }

    private void OnWindowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        try
        {
            switch (e.PropertyName)
            {
                case nameof(WindowState):
                    HandleWindowStateChange();
                    break;
                case nameof(Width):
                case nameof(Height):
                    HandleSizeChange();
                    break;
            }
        }
        catch (Exception ex)
        {
            _settingsService.LogAsync($"Error handling window property change: {ex.Message}").Wait();
        }
    }

    private void HandleWindowStateChange()
    {
        try
        {
            switch (this.WindowState)
            {
                case WindowState.Maximized:
                    _isMaximized = true;
                    break;
                case WindowState.Normal:
                    _isMaximized = false;
                    break;
                case WindowState.Minimized:
                    // Handle minimize if needed
                    break;
            }
        }
        catch (Exception ex)
        {
            _settingsService.LogAsync($"Error handling window state change: {ex.Message}").Wait();
        }
    }

    private void HandleSizeChange()
    {
        try
        {
            // Update view model with current size
            _viewModel.WindowSize = new System.Windows.Size(this.Width, this.Height);
        }
        catch (Exception ex)
        {
            _settingsService.LogAsync($"Error handling window size change: {ex.Message}").Wait();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        try
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.Title):
                    this.Title = _viewModel.Title;
                    break;
                case nameof(MainViewModel.IsLoading):
                    UpdateLoadingState();
                    break;
            }
        }
        catch (Exception ex)
        {
            _settingsService.LogAsync($"Error handling view model property change: {ex.Message}").Wait();
        }
    }

    private void UpdateLoadingState()
    {
        try
        {
            // Update UI based on loading state
            Dispatcher.UIThread.Post(() =>
            {
                var loadingOverlay = this.Find<Border>("LoadingOverlay");
                if (loadingOverlay != null)
                {
                    loadingOverlay.IsVisible = _viewModel.IsLoading;
                }
            });
        }
        catch (Exception ex)
        {
            _settingsService.LogAsync($"Error updating loading state: {ex.Message}").Wait();
        }
    }

    private async void OnRequestClose(object? sender, EventArgs e)
    {
        try
        {
            await this.CloseAsync();
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error handling close request: {ex.Message}");
        }
    }

    private async void OnRequestMinimize(object? sender, EventArgs e)
    {
        try
        {
            this.WindowState = WindowState.Minimized;
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error handling minimize request: {ex.Message}");
        }
    }

    private async void OnRequestMaximize(object? sender, EventArgs e)
    {
        try
        {
            if (_isMaximized)
            {
                await _viewModel.RestoreWindowAsync();
            }
            else
            {
                await _viewModel.MaximizeWindowAsync();
            }
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error handling maximize request: {ex.Message}");
        }
    }

    private async void OnRequestRestore(object? sender, EventArgs e)
    {
        try
        {
            await _viewModel.RestoreWindowAsync();
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error handling restore request: {ex.Message}");
        }
    }

    private async void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        try
        {
            // Update window theme
            this.RequestedThemeVariant = e.NewTheme;
            
            // Update all child controls
            await UpdateChildThemesAsync(e.NewTheme);
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error handling theme change: {ex.Message}");
        }
    }

    private async Task UpdateChildThemesAsync(ThemeVariant newTheme)
    {
        try
        {
            // Update all controls in the visual tree
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateControlThemes(this, newTheme);
            });
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error updating child themes: {ex.Message}");
        }
    }

    private void UpdateControlThemes(Control control, ThemeVariant theme)
    {
        try
        {
            // Update current control
            control.RequestedThemeVariant = theme;

            // Update all child controls
            foreach (var child in control.GetVisualChildren())
            {
                if (child is Control childControl)
                {
                    UpdateControlThemes(childControl, theme);
                }
            }
        }
        catch (Exception ex)
        {
            _settingsService.LogAsync($"Error updating control themes: {ex.Message}").Wait();
        }
    }

    public async Task OnThemeChangedAsync(ThemeVariant newTheme)
    {
        try
        {
            // Handle theme change for this window
            await OnThemeChanged(this, new ThemeChangedEventArgs(_themeManager.CurrentTheme, newTheme));
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error handling theme change in window: {ex.Message}");
        }
    }

    // Public methods for window management
    public async Task ShowAsync()
    {
        try
        {
            await this.ShowDialogAsync();
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error showing window: {ex.Message}");
        }
    }

    public async Task CloseAsync()
    {
        try
        {
            await this.Close();
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error closing window: {ex.Message}");
        }
    }

    public void BringToFront()
    {
        try
        {
            this.WindowState = WindowState.Normal;
            this.Activate();
        }
        catch (Exception ex)
        {
            _settingsService.LogAsync($"Error bringing window to front: {ex.Message}").Wait();
        }
    }

    public void SendToBack()
    {
        try
        {
            this.Deactivate();
        }
        catch (Exception ex)
        {
            _settingsService.LogAsync($"Error sending window to back: {ex.Message}").Wait();
        }
    }
}