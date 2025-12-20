using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace RamOptimizerNova.Controls;

public partial class NavigationItem : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<NavigationItem, string?>(nameof(Title));
    
    public static readonly StyledProperty<IBrush?> IconProperty =
        AvaloniaProperty.Register<NavigationItem, IBrush?>(nameof(Icon));
    
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<NavigationItem, bool>(nameof(IsSelected));
    
    public static readonly StyledProperty<bool> IsEnabledProperty =
        AvaloniaProperty.Register<NavigationItem, bool>(nameof(IsEnabled), true);
    
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<NavigationItem, CornerRadius>(nameof(CornerRadius));
    
    public static readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<NavigationItem, Thickness>(nameof(Padding));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IBrush? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsEnabled
    {
        get => GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public NavigationItem()
    {
        InitializeComponent();
        
        // Handle selection changes
        this.GetObservable(IsSelectedProperty).Subscribe(selected =>
        {
            UpdateSelectionState();
        });
        
        // Handle enabled changes
        this.GetObservable(IsEnabledProperty).Subscribe(enabled =>
        {
            UpdateEnabledState();
        });
        
        // Handle click events
        this.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
    }
    
    private void UpdateSelectionState()
    {
        // This would update the control's appearance based on selection state
        // In a real implementation, this would modify the control's appearance
    }
    
    private void UpdateEnabledState()
    {
        // This would update the control's appearance based on enabled state
        // In a real implementation, this would modify the control's appearance
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsEnabled)
            return;
            
        // Raise a click event or notify parent of selection
        OnClick();
    }
    
    protected virtual void OnClick()
    {
        // This could be raised as a routed event or handled by the parent
        // For now, we'll just update the selection state
        IsSelected = true;
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateSelectionState();
        UpdateEnabledState();
    }
}