using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace RamOptimizerNova.Controls;

public partial class NavigationFrame : UserControl
{
    public static readonly StyledProperty<Control?> CurrentContentProperty =
        AvaloniaProperty.Register<NavigationFrame, Control?>(nameof(CurrentContent));
    
    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<NavigationFrame, bool>(nameof(IsLoading));
    
    public static readonly StyledProperty<string> LoadingTextProperty =
        AvaloniaProperty.Register<NavigationFrame, string>(nameof(LoadingText), "Loading...");
    
    public static readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<NavigationFrame, Thickness>(nameof(Padding));

    public Control? CurrentContent
    {
        get => GetValue(CurrentContentProperty);
        set => SetValue(CurrentContentProperty, value);
    }

    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public string LoadingText
    {
        get => GetValue(LoadingTextProperty);
        set => SetValue(LoadingTextProperty, value);
    }

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    private DispatcherTimer? _transitionTimer;
    private Control? _previousContent;

    public NavigationFrame()
    {
        InitializeComponent();
        
        // Initialize transition timer
        _transitionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        
        _transitionTimer.Tick += OnTransitionTimerTick;
        
        // Handle content changes
        this.GetObservable(CurrentContentProperty).Subscribe(content =>
        {
            OnContentChanged(content);
        });
        
        // Handle loading state changes
        this.GetObservable(IsLoadingProperty).Subscribe(loading =>
        {
            OnLoadingStateChanged(loading);
        });
    }
    
    private void OnContentChanged(Control? newContent)
    {
        if (newContent == null)
            return;
            
        // Start transition
        StartTransition(newContent);
    }
    
    private void StartTransition(Control? newContent)
    {
        if (_transitionTimer != null)
        {
            _transitionTimer.Stop();
        }
        
        // Store previous content
        _previousContent = CurrentContent;
        
        // Set new content
        CurrentContent = newContent;
        
        // Start transition timer
        if (_transitionTimer != null)
        {
            _transitionTimer.Start();
        }
    }
    
    private void OnTransitionTimerTick(object? sender, EventArgs e)
    {
        if (_transitionTimer == null)
            return;
            
        _transitionTimer.Stop();
        
        // Apply transition animation
        if (CurrentContent != null)
        {
            CurrentContent.Classes.Add("transitioning");
        }
        
        // Clean up previous content
        _previousContent = null;
    }
    
    private void OnLoadingStateChanged(bool isLoading)
    {
        // Handle loading state changes
        if (isLoading)
        {
            // Show loading overlay
            if (CurrentContent != null)
            {
                CurrentContent.Opacity = 0.3;
            }
        }
        else
        {
            // Hide loading overlay
            if (CurrentContent != null)
            {
                CurrentContent.Opacity = 1;
            }
        }
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        // Set default padding
        Padding = new Thickness(24);
    }
    
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        // Clean up transition timer
        _transitionTimer?.Stop();
        _transitionTimer = null;
    }
    
    public async Task NavigateToAsync(Control? newContent, bool showLoading = true)
    {
        if (newContent == null)
            return;
            
        try
        {
            if (showLoading)
            {
                IsLoading = true;
                LoadingText = "Loading...";
            }
            
            // Start navigation transition
            StartTransition(newContent);
            
            // Simulate loading time
            await Task.Delay(500);
            
            if (showLoading)
            {
                IsLoading = false;
            }
        }
        catch (Exception ex)
        {
            // Handle navigation errors
            LoadingText = "Navigation failed";
            await Task.Delay(2000);
            IsLoading = false;
        }
    }
    
    public void Refresh()
    {
        // Refresh current content
        if (CurrentContent != null)
        {
            StartTransition(CurrentContent);
        }
    }
}