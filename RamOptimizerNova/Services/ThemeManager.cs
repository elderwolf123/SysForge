using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimeViews;
using Avalonia.Styling;
using RamOptimizerNova.ViewModels;

namespace RamOptimizerNova.Services;

public interface IThemeManager
{
    ThemeVariant CurrentTheme { get; }
    Task SetThemeAsync(ThemeVariant theme);
    Task ToggleThemeAsync();
    Task<string> GetThemeNameAsync();
    Task<string> GetThemeDescriptionAsync();
    IReadOnlyList<ThemeVariant> AvailableThemes { get; }
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
}

public class ThemeChangedEventArgs : EventArgs
{
    public ThemeVariant OldTheme { get; }
    public ThemeVariant NewTheme { get; }
    public DateTime Timestamp { get; }

    public ThemeChangedEventArgs(ThemeVariant oldTheme, ThemeVariant newTheme)
    {
        OldTheme = oldTheme;
        NewTheme = newTheme;
        Timestamp = DateTime.Now;
    }
}

public class ThemeManager : IThemeManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISettingsService _settingsService;
    private ThemeVariant _currentTheme;
    private bool _isInitialized = false;

    public ThemeVariant CurrentTheme => _currentTheme;
    public IReadOnlyList<ThemeVariant> AvailableThemes { get; } = new List<ThemeVariant>
    {
        ThemeVariant.Light,
        ThemeVariant.Dark,
        ThemeVariant.Default
    };

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public ThemeManager(IServiceProvider serviceProvider, ISettingsService settingsService)
    {
        _serviceProvider = serviceProvider;
        _settingsService = settingsService;
        
        // Set default theme
        _currentTheme = ThemeVariant.Default;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            // Load theme from settings
            var savedTheme = await _settingsService.GetSettingAsync<string>("Theme", "Default");
            await SetThemeAsync(savedTheme);
            
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            // Log error but continue with default theme
            await _settingsService.LogAsync($"Error initializing theme: {ex.Message}");
            _currentTheme = ThemeVariant.Default;
            _isInitialized = true;
        }
    }

    public async Task SetThemeAsync(ThemeVariant theme)
    {
        var oldTheme = _currentTheme;
        
        try
        {
            _currentTheme = theme;
            
            // Apply theme to all application windows
            await ApplyThemeToApplicationAsync(theme);
            
            // Save theme to settings
            await _settingsService.SetSettingAsync("Theme", theme.ToString());
            
            // Raise theme changed event
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme));
            
            // Notify UI components of theme change
            await NotifyThemeChangeAsync(theme);
        }
        catch (Exception ex)
        {
            // Revert to old theme on error
            _currentTheme = oldTheme;
            await _settingsService.LogAsync($"Error setting theme: {ex.Message}");
            throw;
        }
    }

    public async Task SetThemeAsync(string themeName)
    {
        if (Enum.TryParse<ThemeVariant>(themeName, true, out var theme))
        {
            await SetThemeAsync(theme);
        }
        else
        {
            throw new ArgumentException($"Invalid theme name: {themeName}");
        }
    }

    public async Task ToggleThemeAsync()
    {
        var nextTheme = GetNextTheme(_currentTheme);
        await SetThemeAsync(nextTheme);
    }

    public async Task<string> GetThemeNameAsync()
    {
        return await Task.FromResult(GetThemeName(_currentTheme));
    }

    public async Task<string> GetThemeDescriptionAsync()
    {
        return await Task.FromResult(GetThemeDescription(_currentTheme));
    }

    private async Task ApplyThemeToApplicationAsync(ThemeVariant theme)
    {
        try
        {
            // Get the current application
            var application = Application.Current;
            if (application == null)
                return;

            // Apply theme to the application
            application.RequestedThemeVariant = theme;
            
            // Apply theme to all application windows
            if (application.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                foreach (var window in desktopLifetime.Windows)
                {
                    if (window is Window win)
                    {
                        win.RequestedThemeVariant = theme;
                    }
                }
            }
            
            // Apply theme to all top-level controls
            await ApplyThemeToControlsAsync(theme);
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error applying theme: {ex.Message}");
            throw;
        }
    }

    private async Task ApplyThemeToControlsAsync(ThemeVariant theme)
    {
        try
        {
            // Get all controls in the application
            var controls = GetAllControls();
            
            foreach (var control in controls)
            {
                if (control is Control ctrl)
                {
                    ctrl.RequestedThemeVariant = theme;
                }
            }
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error applying theme to controls: {ex.Message}");
        }
    }

    private IEnumerable<Control> GetAllControls()
    {
        var controls = new List<Control>();
        
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                foreach (var window in desktopLifetime.Windows)
                {
                    if (window is Window win)
                    {
                        controls.AddRange(GetControlsFromVisualTree(win));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but continue
            Application.Current?.Dispatcher?.PostAsync(async () =>
            {
                await _settingsService.LogAsync($"Error getting controls: {ex.Message}");
            });
        }
        
        return controls;
    }

    private IEnumerable<Control> GetControlsFromVisualTree(Visual visual)
    {
        var controls = new List<Control>();
        
        if (visual is Control control)
        {
            controls.Add(control);
        }
        
        if (visual is IContentControl contentControl && contentControl.Content is Visual contentVisual)
        {
            controls.AddRange(GetControlsFromVisualTree(contentVisual));
        }
        
        if (visual is IItemsControl itemsControl)
        {
            foreach (var item in itemsControl.Items)
            {
                if (item is Visual itemVisual)
                {
                    controls.AddRange(GetControlsFromVisualTree(itemVisual));
                }
            }
        }
        
        if (visual is Visual visualElement)
        {
            foreach (var child in visualElement.GetVisualChildren())
            {
                controls.AddRange(GetControlsFromVisualTree(child));
            }
        }
        
        return controls;
    }

    private async Task NotifyThemeChangeAsync(ThemeVariant theme)
    {
        try
        {
            // Notify all view models of theme change
            var viewModels = GetAllViewModels();
            
            foreach (var viewModel in viewModels)
            {
                if (viewModel is IThemeAwareViewModel themeAwareViewModel)
                {
                    await themeAwareViewModel.OnThemeChangedAsync(theme);
                }
            }
        }
        catch (Exception ex)
        {
            await _settingsService.LogAsync($"Error notifying theme change: {ex.Message}");
        }
    }

    private IEnumerable<ViewModelBase> GetAllViewModels()
    {
        var viewModels = new List<ViewModelBase>();
        
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                foreach (var window in desktopLifetime.Windows)
                {
                    if (window.DataContext is ViewModelBase viewModel)
                    {
                        viewModels.Add(viewModel);
                        viewModels.AddRange(GetViewModelsFromViewModel(viewModel));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but continue
            Application.Current?.Dispatcher?.PostAsync(async () =>
            {
                await _settingsService.LogAsync($"Error getting view models: {ex.Message}");
            });
        }
        
        return viewModels;
    }

    private IEnumerable<ViewModelBase> GetViewModelsFromViewModel(ViewModelBase viewModel)
    {
        var viewModels = new List<ViewModelBase>();
        
        // Get all properties that might contain view models
        var properties = viewModel.GetType().GetProperties();
        
        foreach (var property in properties)
        {
            if (typeof(ViewModelBase).IsAssignableFrom(property.PropertyType))
            {
                var value = property.GetValue(viewModel);
                if (value is ViewModelBase nestedViewModel)
                {
                    viewModels.Add(nestedViewModel);
                    viewModels.AddRange(GetViewModelsFromViewModel(nestedViewModel));
                }
            }
        }
        
        return viewModels;
    }

    private ThemeVariant GetNextTheme(ThemeVariant currentTheme)
    {
        return currentTheme switch
        {
            ThemeVariant.Light => ThemeVariant.Dark,
            ThemeVariant.Dark => ThemeVariant.Default,
            ThemeVariant.Default => ThemeVariant.Light,
            _ => ThemeVariant.Default
        };
    }

    private string GetThemeName(ThemeVariant theme)
    {
        return theme switch
        {
            ThemeVariant.Light => "Light",
            ThemeVariant.Dark => "Dark",
            ThemeVariant.Default => "System",
            _ => "Unknown"
        };
    }

    private string GetThemeDescription(ThemeVariant theme)
    {
        return theme switch
        {
            ThemeVariant.Light => "Light theme with clean, modern design",
            ThemeVariant.Dark => "Dark theme with space-inspired aesthetics",
            ThemeVariant.Default => "Use system theme settings",
            _ => "Unknown theme"
        };
    }
}

public interface IThemeAwareViewModel
{
    Task OnThemeChangedAsync(ThemeVariant newTheme);
}