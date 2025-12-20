using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace RamOptimizerNova.Controls;

public partial class ModernButton : UserControl
{
    public static readonly StyledProperty<string?> ContentProperty =
        AvaloniaProperty.Register<ModernButton, string?>(nameof(Content));
    
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<ModernButton, IBrush?>(nameof(Background));
    
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<ModernButton, IBrush?>(nameof(BorderBrush));
    
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<ModernButton, CornerRadius>(nameof(CornerRadius));
    
    public static readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<ModernButton, Thickness>(nameof(Padding));
    
    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        AvaloniaProperty.Register<ModernButton, FontWeight>(nameof(FontWeight));
    
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<ModernButton, IBrush?>(nameof(Foreground));
    
    public static readonly StyledProperty<bool> IsEnabledProperty =
        AvaloniaProperty.Register<ModernButton, bool>(nameof(IsEnabled), true);
    
    public static readonly StyledProperty<ButtonVariant> VariantProperty =
        AvaloniaProperty.Register<ModernButton, ButtonVariant>(nameof(Variant), ButtonVariant.Primary);

    public string? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
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

    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public bool IsEnabled
    {
        get => GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    public ButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public ModernButton()
    {
        InitializeComponent();
        
        // Handle variant changes
            this.GetObservable(VariantProperty).Subscribe(variant =>
        {
            UpdateVariantStyles();
        });
        
        // Handle enabled changes
        this.GetObservable(IsEnabledProperty).Subscribe(enabled =>
        {
            UpdateEnabledState();
        });
    }
    
    private void UpdateVariantStyles()
    {
        // This would apply variant-specific styles
        // In a real implementation, this would modify the control's appearance
    }
    
    private void UpdateEnabledState()
    {
        // This would update the control's appearance based on enabled state
        // In a real implementation, this would modify the control's appearance
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateVariantStyles();
        UpdateEnabledState();
    }
}

public enum ButtonVariant
{
    Primary,
    Secondary,
    Danger,
    Success
}